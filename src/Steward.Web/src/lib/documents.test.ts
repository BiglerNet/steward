import { describe, expect, it } from "vitest";
import { MAX_DOCUMENT_SIZE_BYTES, validateDocumentFile } from "@/lib/documents";

function makeFile(name: string, type: string, size: number): File {
  const file = new File([new Uint8Array(size)], name, { type });
  return file;
}

describe("validateDocumentFile", () => {
  it("accepts an allowed content type within the size limit", () => {
    const file = makeFile("doc.pdf", "application/pdf", 1024);
    expect(validateDocumentFile(file)).toBeNull();
  });

  it("rejects an unsupported content type", () => {
    const file = makeFile("doc.txt", "text/plain", 1024);
    expect(validateDocumentFile(file)).toMatch(/unsupported file type/i);
  });

  it("rejects a file exceeding the maximum size", () => {
    const file = makeFile("doc.pdf", "application/pdf", MAX_DOCUMENT_SIZE_BYTES + 1);
    expect(validateDocumentFile(file)).toMatch(/maximum allowed size/i);
  });

  it("accepts a file exactly at the maximum size", () => {
    const file = makeFile("doc.pdf", "application/pdf", MAX_DOCUMENT_SIZE_BYTES);
    expect(validateDocumentFile(file)).toBeNull();
  });
});
