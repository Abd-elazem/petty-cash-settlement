import { createBrowserRouter } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { ManagerRoute } from "./auth/ManagerRoute";
import { AppLayout } from "./layout/AppLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { MySettlementsPage } from "./pages/MySettlementsPage";
import { NewSettlementPage } from "./pages/NewSettlementPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ManagerInboxPage } from "./pages/ManagerInboxPage";
import { ManagerSettlementDetailPage } from "./pages/ManagerSettlementDetailPage";
import { SettlementDetailPage } from "./pages/SettlementDetailPage";
import { SignInPage } from "./pages/SignInPage";
import { UnauthorizedPage } from "./pages/UnauthorizedPage";

export const appRouter = createBrowserRouter([
  {
    path: "/signin",
    element: <SignInPage />
  },
  {
    path: "/unauthorized",
    element: <UnauthorizedPage />
  },
  {
    path: "/",
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      {
        index: true,
        element: <DashboardPage />
      },
      {
        path: "my-settlements",
        element: <MySettlementsPage />
      },
      {
        path: "my-settlements/:requestId",
        element: <SettlementDetailPage />
      },
      {
        path: "settlements/new",
        element: <NewSettlementPage />
      },
      {
        path: "manager-inbox",
        element: (
          <ManagerRoute>
            <ManagerInboxPage />
          </ManagerRoute>
        )
      },
      {
        path: "manager-inbox/:requestId",
        element: (
          <ManagerRoute>
            <ManagerSettlementDetailPage />
          </ManagerRoute>
        )
      }
    ]
  },
  {
    path: "*",
    element: <NotFoundPage />
  }
]);

