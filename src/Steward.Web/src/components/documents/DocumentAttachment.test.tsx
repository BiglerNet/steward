import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AxiosError } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { toast } from "sonner";
import * as documentsApi from "@/api/documents";
import { DocumentAttachment } from "@/components/documents/DocumentAttachment";

vi.mock("@/api/documents");
vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

function renderWidget(props: Partial<Parameters<typeof DocumentAttachment>[0]> = {}) {
  const queryClient = new QueryClient();
  const onChange = props.onChange ?? vi.fn();
  return {
    onChange,
    ...render(
      <QueryClientProvider client={queryClient}>
        <DocumentAttachment
          hasDocument={false}
          uploadUrl="/api/registrations/1/document"
          downloadUrl="/api/registrations/1/document"
          deleteUrl="/api/registrations/1/document"
          canEdit
          onChange={onChange}
          {...props}
        />
      </QueryClientProvider>
    ),
  };
}

describe("DocumentAttachment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("uploads a valid file and notifies the parent", async () => {
    vi.mocked(documentsApi.uploadDocument).mockResolvedValue({});
    const { onChange } = renderWidget();
    const user = userEvent.setup();

    const file = new File(["content"], "doc.pdf", { type: "application/pdf" });
    const input = screen.getByLabelText("Document file");
    await user.upload(input, file);

    await waitFor(() =>
      expect(documentsApi.uploadDocument).toHaveBeenCalledWith("/api/registrations/1/document", file)
    );
    await waitFor(() => expect(onChange).toHaveBeenCalled());
  });

  it("rejects an unsupported file without calling the API", async () => {
    const { onChange } = renderWidget();
    const user = userEvent.setup({ applyAccept: false });

    const file = new File(["content"], "doc.txt", { type: "text/plain" });
    const input = screen.getByLabelText("Document file");
    await user.upload(input, file);

    expect(await screen.findByText(/unsupported file type/i)).toBeInTheDocument();
    expect(documentsApi.uploadDocument).not.toHaveBeenCalled();
    expect(onChange).not.toHaveBeenCalled();
  });

  it("shows Replace and Remove when a document already exists", async () => {
    vi.mocked(documentsApi.removeDocument).mockResolvedValue(undefined);
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const { onChange } = renderWidget({ hasDocument: true });
    const user = userEvent.setup();

    expect(screen.getByRole("button", { name: "Replace" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Remove" }));

    await waitFor(() => expect(documentsApi.removeDocument).toHaveBeenCalledWith("/api/registrations/1/document"));
    await waitFor(() => expect(onChange).toHaveBeenCalled());
  });

  it("shows only the download action for a Viewer", () => {
    renderWidget({ hasDocument: true, canEdit: false });

    expect(screen.getByRole("button", { name: "Download" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Replace" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Remove" })).not.toBeInTheDocument();
  });

  it("surfaces a backend 400 via the global toast and stays in the pre-upload state", async () => {
    const axiosError = new AxiosError("Bad Request");
    axiosError.response = {
      status: 400,
      data: { title: "Unsupported content type." },
      statusText: "Bad Request",
      headers: {},
      config: {} as never,
    };
    vi.mocked(documentsApi.uploadDocument).mockRejectedValue(axiosError);
    const { onChange } = renderWidget();
    const user = userEvent.setup();

    const file = new File(["content"], "doc.pdf", { type: "application/pdf" });
    const input = screen.getByLabelText("Document file");
    await user.upload(input, file);

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith("Unsupported content type."));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Attach document" })).toBeInTheDocument();
  });
});
