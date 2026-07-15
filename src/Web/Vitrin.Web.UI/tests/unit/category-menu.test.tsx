import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CategoryMenu } from "@/components/category-menu";

const store = vi.hoisted(() => ({
  topics: [{ id: "topic-1", name: "Developer Tools", slug: "developer-tools" }],
  selectedTopicSlug: null as string | null,
  fetchTopics: vi.fn(),
  setTopicFilter: vi.fn(),
}));

vi.mock("@/core/application/useProductStore", () => ({
  useProductStore: () => store,
}));

describe("CategoryMenu", () => {
  beforeEach(() => {
    store.topics = [{ id: "topic-1", name: "Developer Tools", slug: "developer-tools" }];
    store.selectedTopicSlug = null;
    store.fetchTopics.mockReset();
    store.setTopicFilter.mockReset();
  });

  it("loads topics and forwards a selected topic slug", async () => {
    const user = userEvent.setup();
    render(<CategoryMenu />);

    await waitFor(() => expect(store.fetchTopics).toHaveBeenCalledOnce());
    await user.click(screen.getByRole("button", { name: "Developer Tools" }));

    expect(store.setTopicFilter).toHaveBeenCalledWith("developer-tools");
  });

  it("renders nothing while there are no topics", () => {
    store.topics = [];
    const { container } = render(<CategoryMenu />);

    expect(container).toBeEmptyDOMElement();
  });
});
