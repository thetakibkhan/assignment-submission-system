import { RoleDashboard } from "@/components/dashboard/RoleDashboard";
import { DarkGradientBg } from "@/components/layout/DarkGradientBg";

export default function AdminPage() {
  return (
    <DarkGradientBg>
      <RoleDashboard endpoint="/api/dashboard/admin" role="Admin" />
    </DarkGradientBg>
  );
}
