export function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error && error.message ? error.message : fallback;
}

export function getApiProblemMessage(payload: unknown, fallback: string): string {
  if (!payload || typeof payload !== "object") return fallback;

  const problem = payload as {
    detail?: unknown;
    title?: unknown;
    errors?: unknown;
  };

  if (problem.errors && typeof problem.errors === "object") {
    const validationMessages = Object.values(problem.errors)
      .flatMap((value) => Array.isArray(value) ? value : [value])
      .filter((value): value is string => typeof value === "string" && value.length > 0);

    if (validationMessages.length > 0) {
      return validationMessages.join(" ");
    }
  }

  if (typeof problem.detail === "string" && problem.detail) return problem.detail;
  if (typeof problem.title === "string" && problem.title) return problem.title;
  return fallback;
}
