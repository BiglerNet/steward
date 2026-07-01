import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { z } from "zod";
import { TrackingLogSection, type TrackingLogSectionProps } from "@/components/tracking/TrackingLogSection";
import { FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";

interface Entry {
  id: string;
  date: string;
  notes: string | null;
}

const schema = z.object({ date: z.string().min(1, "Date is required"), notes: z.string().optional() });
type FormValues = z.infer<typeof schema>;

function renderSection(overrides: Partial<TrackingLogSectionProps<Entry, FormValues>> = {}) {
  const queryClient = new QueryClient();
  const list = overrides.list ?? vi.fn().mockResolvedValue([]);
  const create = overrides.create ?? vi.fn();
  const update = overrides.update ?? vi.fn();
  const remove = overrides.remove ?? vi.fn();

  render(
    <QueryClientProvider client={queryClient}>
      <TrackingLogSection<Entry, FormValues>
        title="Entries"
        emptyMessage="No entries yet."
        queryKey={["test-entries"]}
        canEdit
        columns={[
          { key: "date", header: "Date", render: (r) => r.date },
          { key: "notes", header: "Notes", render: (r) => r.notes ?? "—" },
        ]}
        list={list}
        create={create}
        update={update}
        remove={remove}
        getId={(r) => r.id}
        sortValue={(r) => r.date}
        schema={schema}
        defaultValues={{ date: "", notes: "" }}
        toFormValues={(r) => ({ date: r.date, notes: r.notes ?? "" })}
        renderFields={(form) => (
          <>
            <FormField
              control={form.control}
              name="date"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Date</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="notes"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Notes</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                </FormItem>
              )}
            />
          </>
        )}
        {...overrides}
      />
    </QueryClientProvider>
  );

  return { list, create, update, remove };
}

describe("TrackingLogSection", () => {
  it("shows the empty message when there are no records", async () => {
    renderSection();
    expect(await screen.findByText("No entries yet.")).toBeInTheDocument();
  });

  it("sorts records newest-first by sortValue", async () => {
    renderSection({
      list: vi.fn().mockResolvedValue([
        { id: "e1", date: "2026-01-01", notes: "first" },
        { id: "e2", date: "2026-03-01", notes: "third" },
        { id: "e3", date: "2026-02-01", notes: "second" },
      ]),
    });

    const rows = await screen.findAllByRole("row");
    const bodyRows = rows.slice(1);
    expect(within(bodyRows[0]).getByText("third")).toBeInTheDocument();
    expect(within(bodyRows[1]).getByText("second")).toBeInTheDocument();
    expect(within(bodyRows[2]).getByText("first")).toBeInTheDocument();
  });

  it("creates a new entry through the add dialog", async () => {
    const { create } = renderSection({
      create: vi.fn().mockResolvedValue({ id: "e1", date: "2026-01-01", notes: "hello" }),
    });
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Add entry" }));
    await user.type(screen.getByLabelText("Date"), "2026-01-01");
    await user.type(screen.getByLabelText("Notes"), "hello");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(create).toHaveBeenCalledWith({ date: "2026-01-01", notes: "hello" })
    );
  });

  it("edits and deletes an existing entry", async () => {
    const { update, remove } = renderSection({
      list: vi.fn().mockResolvedValue([{ id: "e1", date: "2026-01-01", notes: "original" }]),
      update: vi.fn().mockResolvedValue({ id: "e1", date: "2026-01-02", notes: "updated" }),
      remove: vi.fn().mockResolvedValue(undefined),
    });
    const user = userEvent.setup();
    vi.spyOn(window, "confirm").mockReturnValue(true);

    await screen.findByText("original");
    await user.click(screen.getByRole("button", { name: "Edit" }));
    const dateInput = screen.getByLabelText("Date");
    await user.clear(dateInput);
    await user.type(dateInput, "2026-01-02");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => expect(update).toHaveBeenCalledWith("e1", { date: "2026-01-02", notes: "original" }));

    await user.click(screen.getByRole("button", { name: "Delete" }));
    await waitFor(() => expect(remove).toHaveBeenCalledWith("e1"));
  });

  it("hides create/edit/delete controls when canEdit is false", async () => {
    renderSection({
      canEdit: false,
      list: vi.fn().mockResolvedValue([{ id: "e1", date: "2026-01-01", notes: "original" }]),
    });

    await screen.findByText("original");
    expect(screen.queryByRole("button", { name: "Add entry" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });
});
