interface DashboardHeaderProps {
    onLogout: () => void;
}

export function DashboardHeader({ onLogout }: DashboardHeaderProps) {
    return (
        <header className="dashboard-header">
            <span className="brand">VibeBox</span>

            <button
                type="button"
                className="secondary-button"
                onClick={onLogout}
            >
                Sign out
            </button>
        </header>
    );
}