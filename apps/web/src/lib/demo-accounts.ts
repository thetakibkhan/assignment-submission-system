export interface DemoAccount {
  institutionalId: string;
  password: string;
  role: string;
}

export const recruiterDemoAccounts: DemoAccount[] = [
  { institutionalId: "DEMO-ADM-001", password: "Admin!nVPT7mB4lxeZBUq2XwI", role: "Administrator" },
  { institutionalId: "DEMO-TCH-001", password: "Teacher!XQt7fpdAg1jQJ7fGgSw", role: "Teacher" },
  { institutionalId: "DEMO-STU-001", password: "Student!v5lVJbrqfRO0m93zrKQ", role: "Student" },
];
