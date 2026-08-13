interface StudentResultVisibilityOptions {
  deadlinePassed: boolean;
  marks: number | null;
  status: string;
}

export function shouldShowStudentResult({
  deadlinePassed,
  marks,
  status,
}: StudentResultVisibilityOptions): boolean {
  return deadlinePassed && status === "Graded" && marks !== null;
}
