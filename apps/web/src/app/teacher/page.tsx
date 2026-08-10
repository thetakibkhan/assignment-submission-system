import { RoleDashboard } from "@/components/dashboard/RoleDashboard";
import { DarkGradientBg } from "@/components/layout/DarkGradientBg";

export default function TeacherPage() {
  return (
    <DarkGradientBg>
      <RoleDashboard endpoint="/api/dashboard/teacher" role="Teacher" />
    </DarkGradientBg>
  );
}
