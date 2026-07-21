import type { ReactNode } from "react";

type FormFieldProps = {
  htmlFor: string;
  label: string;
  error?: string;
  children: ReactNode;
};

export function FormField({ htmlFor, label, error, children }: FormFieldProps): JSX.Element {
  return (
    <label className="settlement-form-field" htmlFor={htmlFor}>
      <span>{label}</span>
      {children}
      {error ? <small className="settlement-form-error">{error}</small> : null}
    </label>
  );
}

