import { RoleDashboard } from "@/components/dashboard/RoleDashboard";
import { DarkGradientBg } from "@/components/layout/DarkGradientBg";

export default function StudentPage() {
  return (
    <DarkGradientBg>
      <RoleDashboard endpoint="/api/dashboard/student" role="Student" />
    </DarkGradientBg>
  );
}
