import type { AssetCategory, TemplateResponse } from "@/api/types";

export function isTemplateApplicable(template: TemplateResponse, category: AssetCategory): boolean {
  return template.applicableCategories.length === 0 || template.applicableCategories.includes(category);
}
