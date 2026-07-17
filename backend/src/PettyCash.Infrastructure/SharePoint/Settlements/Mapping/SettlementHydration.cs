using System.Reflection;
using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.SharePoint.Settlements.Models;

namespace PettyCash.Infrastructure.SharePoint.Settlements.Mapping;

internal static class SettlementHydration
{
    private static readonly ConstructorInfo SettlementConstructor =
        typeof(Settlement).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)
        ?? throw new InvalidOperationException("Settlement private constructor was not found.");

    private static readonly ConstructorInfo SettlementLineConstructor =
        typeof(SettlementLine).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)
        ?? throw new InvalidOperationException("SettlementLine private constructor was not found.");

    private static readonly ConstructorInfo VatBreakdownConstructor =
        typeof(VatBreakdown).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, [typeof(decimal), typeof(decimal), typeof(decimal)], modifiers: null)
        ?? throw new InvalidOperationException("VatBreakdown private constructor was not found.");

    private static readonly FieldInfo SettlementLinesField =
        typeof(Settlement).GetField("_lines", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Settlement backing field '_lines' was not found.");

    public static Settlement CreateAggregate(SharePointSettlementHeaderItem header, IReadOnlyList<SharePointSettlementLineItem> lines)
    {
        var settlement = (Settlement)SettlementConstructor.Invoke(null);

        SetProperty(settlement, nameof(Settlement.Id), header.RequestId);
        SetProperty(settlement, nameof(Settlement.Version), header.Version);
        SetProperty(settlement, nameof(Settlement.SettlementDate), header.SettlementDate);
        SetProperty(settlement, nameof(Settlement.Purpose), header.Purpose);
        SetProperty(settlement, nameof(Settlement.SpenderId), header.SpenderId);
        SetProperty(settlement, nameof(Settlement.SpenderNameSnapshot), header.SpenderNameSnapshot);
        SetProperty(settlement, nameof(Settlement.WorkerIdSnapshot), header.WorkerIdSnapshot);
        SetProperty(settlement, nameof(Settlement.ApproverEmailSnapshot), header.ApproverEmailSnapshot);
        SetProperty(settlement, nameof(Settlement.Status), Enum.Parse<SettlementStatus>(header.Status, ignoreCase: true));
        SetProperty(settlement, nameof(Settlement.ApprovalComment), header.ApprovalComment);
        SetProperty(settlement, nameof(Settlement.JournalBatchNumber), header.JournalBatchNumber);

        var lineCollection = (IList<SettlementLine>)SettlementLinesField.GetValue(settlement)!;
        lineCollection.Clear();

        foreach (var line in lines.OrderBy(l => l.LineNo))
        {
            lineCollection.Add(CreateLine(line));
        }

        settlement.ClearDomainEvents();
        return settlement;
    }

    public static SharePointSettlementHeaderItem ToHeader(Settlement settlement, string itemId, string? eTag)
    {
        return new SharePointSettlementHeaderItem(
            ItemId: itemId,
            ETag: eTag,
            RequestId: settlement.Id,
            Version: settlement.Version,
            SettlementDate: settlement.SettlementDate,
            Purpose: settlement.Purpose,
            SpenderId: settlement.SpenderId,
            SpenderNameSnapshot: settlement.SpenderNameSnapshot,
            WorkerIdSnapshot: settlement.WorkerIdSnapshot,
            ApproverEmailSnapshot: settlement.ApproverEmailSnapshot,
            Status: settlement.Status.ToString(),
            ApprovalComment: settlement.ApprovalComment,
            JournalBatchNumber: settlement.JournalBatchNumber);
    }

    public static IReadOnlyList<SharePointSettlementLineItem> ToLines(Settlement settlement)
    {
        return settlement.Lines
            .Select(line => new SharePointSettlementLineItem(
                ItemId: string.Empty,
                ETag: null,
                RequestId: settlement.Id,
                LineId: line.LineId,
                LineNo: line.LineNo,
                CategoryCode: line.CategoryCode,
                ExpenseMainAccountSnapshot: line.ExpenseMainAccountSnapshot,
                DimensionDefaultsSnapshot: line.DimensionDefaultsSnapshot,
                GrossAmount: line.GrossAmount.Amount,
                Currency: line.GrossAmount.Currency,
                IsVat: line.IsVat,
                VatAmount: line.VatBreakdown.Vat,
                NetAmount: line.VatBreakdown.Net,
                Notes: line.Notes,
                CarPlate: line.Odometer?.CarPlate,
                OdometerKm: line.Odometer?.OdometerKm))
            .OrderBy(line => line.LineNo)
            .ToList();
    }

    private static SettlementLine CreateLine(SharePointSettlementLineItem line)
    {
        var settlementLine = (SettlementLine)SettlementLineConstructor.Invoke(null);
        SetProperty(settlementLine, nameof(SettlementLine.LineId), line.LineId);
        SetProperty(settlementLine, nameof(SettlementLine.LineNo), line.LineNo);
        SetProperty(settlementLine, nameof(SettlementLine.CategoryCode), line.CategoryCode);
        SetProperty(settlementLine, nameof(SettlementLine.ExpenseMainAccountSnapshot), line.ExpenseMainAccountSnapshot);
        SetProperty(settlementLine, nameof(SettlementLine.DimensionDefaultsSnapshot), line.DimensionDefaultsSnapshot);
        SetProperty(settlementLine, nameof(SettlementLine.GrossAmount), new Money(line.GrossAmount, line.Currency));
        SetProperty(settlementLine, nameof(SettlementLine.IsVat), line.IsVat);

        var vatBreakdown = (VatBreakdown)VatBreakdownConstructor.Invoke([line.GrossAmount, line.VatAmount, line.NetAmount]);
        SetProperty(settlementLine, nameof(SettlementLine.VatBreakdown), vatBreakdown);

        SetProperty(settlementLine, nameof(SettlementLine.Notes), line.Notes);
        SetProperty(
            settlementLine,
            nameof(SettlementLine.Odometer),
            line.CarPlate is not null && line.OdometerKm is not null
                ? new OdometerReading(line.CarPlate, line.OdometerKm.Value)
                : null);

        return settlementLine;
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on {instance.GetType().Name}.");

        var setter = property.SetMethod;
        if (setter is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' on {instance.GetType().Name} does not have a setter.");
        }

        setter.Invoke(instance, [value]);
    }
}
