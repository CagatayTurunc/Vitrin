import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProductRow } from "@/components/product-row";
import type { Product } from "@/core/domain/product.types";

const state = vi.hoisted(() => ({
  session: null as null | { accessToken: string },
  upvote: vi.fn(),
  votedProductIds: [] as string[],
}));

vi.mock("next-auth/react", () => ({
  useSession: () => ({ data: state.session }),
}));
vi.mock("@/core/application/useProductStore", () => ({
  useProductStore: () => ({ upvote: state.upvote, votedProductIds: state.votedProductIds }),
}));
vi.mock("next/image", () => ({
  // eslint-disable-next-line @next/next/no-img-element -- the optimized component is replaced only inside jsdom.
  default: ({ src, alt }: { src: string; alt: string }) => <img src={src} alt={alt} />,
}));
vi.mock("@/components/login-modal", () => ({
  LoginModal: ({ isOpen }: { isOpen: boolean }) => isOpen ? <div role="dialog">Authentication required</div> : null,
}));

const product: Product = {
  id: "product-1",
  rank: 1,
  name: "Test Product",
  slug: "test-product",
  description: "A product used by the component test.",
  publishedAt: "2026-07-15T00:00:00Z",
  image: "/test-product.png",
  topics: [{ id: "topic-1", name: "Testing", slug: "testing" }],
  votes: 42,
};

describe("ProductRow", () => {
  beforeEach(() => {
    state.session = null;
    state.votedProductIds = [];
    state.upvote.mockReset();
  });

  it("opens login instead of trusting an unauthenticated vote", async () => {
    const user = userEvent.setup();
    render(<ProductRow product={product} />);

    await user.click(screen.getByRole("button", { name: /Test Product/ }));

    expect(screen.getByRole("dialog")).toBeVisible();
    expect(state.upvote).not.toHaveBeenCalled();
  });

  it("passes the authenticated access token to the vote action", async () => {
    state.session = { accessToken: "trusted-access-token" };
    const user = userEvent.setup();
    render(<ProductRow product={product} />);

    await user.click(screen.getByRole("button", { name: /Test Product/ }));

    expect(state.upvote).toHaveBeenCalledWith("product-1", "trusted-access-token");
  });
});
