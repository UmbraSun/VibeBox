import { Navigate, Route, Routes } from "react-router-dom";
import { LoginPage } from "@/features/auth/pages/LoginPage";
import { RegisterPage } from "@/features/auth/pages/RegisterPage";
import { ProtectedRoute } from "@/features/auth/components/ProtectedRoute";
import { DashboardPage } from "@/features/dashboard/pages/DashboardPage";

function App() {
    return (
        <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            <Route element={<ProtectedRoute />}>
                <Route path="/dashboard" element={<DashboardPage />} />
            </Route>

            <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
    );
}

export default App;