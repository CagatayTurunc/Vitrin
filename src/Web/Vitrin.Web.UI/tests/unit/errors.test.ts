import { describe, expect, it } from "vitest";
import { getApiProblemMessage, getErrorMessage } from "@/lib/errors";

describe("error helpers", () => {
  it("uses an Error message and otherwise falls back", () => {
    expect(getErrorMessage(new Error("network unavailable"), "fallback")).toBe("network unavailable");
    expect(getErrorMessage({ message: "untrusted" }, "fallback")).toBe("fallback");
  });

  it("prioritizes validation messages from ProblemDetails", () => {
    const message = getApiProblemMessage(
      {
        title: "Validation failed",
        detail: "Request is invalid",
        errors: { email: ["Email is required."], password: "Password is too short." },
      },
      "fallback",
    );

    expect(message).toBe("Email is required. Password is too short.");
  });

  it("falls back through detail, title, and the caller fallback", () => {
    expect(getApiProblemMessage({ detail: "Detailed failure", title: "Failure" }, "fallback")).toBe("Detailed failure");
    expect(getApiProblemMessage({ title: "Failure" }, "fallback")).toBe("Failure");
    expect(getApiProblemMessage(null, "fallback")).toBe("fallback");
  });
});
