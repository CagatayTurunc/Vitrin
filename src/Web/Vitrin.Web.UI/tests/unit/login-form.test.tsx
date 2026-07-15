import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginForm } from "@/components/auth/login-form";

const mocks = vi.hoisted(() => ({
  signIn: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock("next-auth/react", () => ({ signIn: mocks.signIn }));
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: mocks.push, refresh: mocks.refresh }),
}));

describe("LoginForm", () => {
  beforeEach(() => {
    mocks.signIn.mockReset();
    mocks.push.mockReset();
    mocks.refresh.mockReset();
  });

  it("submits credentials and navigates after a successful login", async () => {
    mocks.signIn.mockResolvedValue({ ok: true, error: null });
    const user = userEvent.setup();
    const { container } = render(<LoginForm />);

    await user.type(screen.getByLabelText("E-posta"), "user@example.com");
    await user.type(container.querySelector<HTMLInputElement>("#password")!, "correct-password");
    await user.click(container.querySelector<HTMLButtonElement>('button[type="submit"]')!);

    await waitFor(() => expect(mocks.signIn).toHaveBeenCalledWith("credentials", {
      redirect: false,
      email: "user@example.com",
      password: "correct-password",
    }));
    expect(mocks.push).toHaveBeenCalledWith("/");
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it("shows an error and keeps the user on the page when credentials are rejected", async () => {
    mocks.signIn.mockResolvedValue({ ok: false, error: "CredentialsSignin" });
    const { container } = render(<LoginForm />);

    fireEvent.change(screen.getByLabelText("E-posta"), { target: { value: "user@example.com" } });
    fireEvent.change(container.querySelector<HTMLInputElement>("#password")!, { target: { value: "wrong" } });
    fireEvent.submit(container.querySelector("form")!);

    await waitFor(() => expect(container.querySelector(".text-destructive")).toBeVisible());
    expect(mocks.push).not.toHaveBeenCalled();
  });

  it("starts the selected OAuth flow with a safe local callback", async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    await user.click(screen.getByRole("button", { name: "Google" }));
    expect(mocks.signIn).toHaveBeenCalledWith("google", { callbackUrl: "/" });
  });

  it("exposes an accessible password visibility control", async () => {
    const user = userEvent.setup();
    const { container } = render(<LoginForm />);
    const password = container.querySelector<HTMLInputElement>("#password")!;

    await user.click(screen.getByRole("button", { name: "Şifreyi göster" }));

    expect(password).toHaveAttribute("type", "text");
    expect(screen.getByRole("button", { name: "Şifreyi gizle" })).toBeVisible();
  });
});
