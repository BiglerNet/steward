import { describe, expect, it } from "vitest";
import { assetTypeFieldConfig, clearInapplicableFields } from "@/lib/assetTypeFieldConfig";

describe("assetTypeFieldConfig", () => {
  it("groups vehicle types around vin/color/make/model", () => {
    expect(assetTypeFieldConfig.Car.map((f) => f.key)).toEqual(["vin", "color", "make", "model"]);
    expect(assetTypeFieldConfig.Truck.map((f) => f.key)).toEqual(["vin", "color", "make", "model"]);
    expect(assetTypeFieldConfig.Snowmobile.map((f) => f.key)).toEqual([
      "vin",
      "color",
      "make",
      "model",
      "trackLengthIn",
    ]);
  });

  it("excludes vin from Boat fields", () => {
    expect(assetTypeFieldConfig.Boat.map((f) => f.key)).not.toContain("vin");
    expect(assetTypeFieldConfig.Boat.map((f) => f.key)).toContain("hin");
  });
});

describe("clearInapplicableFields", () => {
  it("keeps and parses values for fields applicable to the asset type", () => {
    const result = clearInapplicableFields("Car", {
      vin: "1HGCM82633A123456",
      color: "Red",
      make: "Honda",
      model: "Civic",
    });

    expect(result).toMatchObject({
      vin: "1HGCM82633A123456",
      color: "Red",
      make: "Honda",
      model: "Civic",
    });
  });

  it("nulls out fields that don't belong to the selected type's field group", () => {
    const result = clearInapplicableFields("RidingMower", {
      vin: "should-be-cleared",
      color: "should-be-cleared",
      cuttingWidthIn: "42",
    });

    expect(result.vin).toBeNull();
    expect(result.color).toBeNull();
    expect(result.cuttingWidthIn).toBe(42);
  });

  it("parses numeric fields to numbers and leaves them null when blank", () => {
    const result = clearInapplicableFields("PowerWasher", {
      maxPsi: "3000",
      maxGpm: "",
    });

    expect(result.maxPsi).toBe(3000);
    expect(result.maxGpm).toBeNull();
  });
});
