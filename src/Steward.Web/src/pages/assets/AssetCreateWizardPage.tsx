import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Navigate, useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import { createAsset } from "@/api/assets";
import { createEngine } from "@/api/engines";
import type { AssetCategory, AssetResponse, AssetTypeDefinition, CreateAssetRequest, CreateEngineRequest, VinDecodeResult } from "@/api/types";
import { decodeVin } from "@/api/vinDecode";
import { DetailsStep } from "@/components/assets/wizard/DetailsStep";
import { EngineStep } from "@/components/assets/wizard/EngineStep";
import { PhotosStep } from "@/components/assets/wizard/PhotosStep";
import { TypeStep } from "@/components/assets/wizard/TypeStep";
import { VinStep } from "@/components/assets/wizard/VinStep";
import { WizardStepper } from "@/components/assets/wizard/WizardStepper";
import { useAssetTypeRegistry } from "@/hooks/useAssetTypeRegistry";
import { useHouseholds } from "@/hooks/useHouseholds";
import {
  ALL_ASSET_TYPE_FIELD_KEYS,
  type AssetFieldsFormValues,
  assetFieldsSchema,
  clearInapplicableFields,
  findDefinition,
} from "@/lib/assetTypes";
import {
  defaultEngineFieldsValues,
  engineFieldsSchema,
  isHybridChoice,
  type EngineFieldsFormValues,
} from "@/lib/engineFields";
import { parseOptionalNumber, parseOptionalText } from "@/lib/formHelpers";
import { canEditWithRole } from "@/lib/permissions";
import { ftLbsToNm } from "@/lib/units";
import { bodyClassMismatchHint, mapFuelTypePrimary } from "@/lib/vinDecodeMapping";

type WizardStepId = "type" | "vin" | "details" | "engine" | "photos";

const STEP_LABELS: Record<WizardStepId, string> = {
  type: "Type",
  vin: "VIN",
  details: "Details",
  engine: "Engine",
  photos: "Photos",
};

function stepsForDefinition(definition: AssetTypeDefinition | undefined): WizardStepId[] {
  const steps: WizardStepId[] = ["type"];
  if (definition && definition.vinDecodeSupport !== "None") {
    steps.push("vin");
  }
  steps.push("details");
  if (definition?.typicallyHasEngine) {
    steps.push("engine");
  }
  steps.push("photos");
  return steps;
}

const EMPTY_DETAILS_VALUES: AssetFieldsFormValues = {
  name: "",
  description: "",
  year: "",
  usageTrackingMode: "None",
  ...Object.fromEntries(ALL_ASSET_TYPE_FIELD_KEYS.map((key) => [key, ""])),
} as AssetFieldsFormValues;

export function AssetCreateWizardPage() {
  const { householdId } = useParams() as { householdId: string };
  const navigate = useNavigate();
  const { data: households, isLoading: householdsLoading } = useHouseholds();
  const { data: registry } = useAssetTypeRegistry();

  const [category, setCategory] = useState<AssetCategory | undefined>(undefined);
  const [stepIndex, setStepIndex] = useState(0);
  const [vin, setVin] = useState("");
  const [decodeResult, setDecodeResult] = useState<VinDecodeResult | null>(null);
  const [decodeFailed, setDecodeFailed] = useState(false);
  const [createdAsset, setCreatedAsset] = useState<AssetResponse | null>(null);
  const [engineError, setEngineError] = useState<string | null>(null);
  const [createdIceEngineId, setCreatedIceEngineId] = useState<string | null>(null);
  const [createdElectricEngineId, setCreatedElectricEngineId] = useState<string | null>(null);

  const definition = findDefinition(registry, category);
  const steps = stepsForDefinition(definition);
  const currentStep = steps[stepIndex];

  const detailsForm = useForm<AssetFieldsFormValues>({
    resolver: zodResolver(assetFieldsSchema),
    defaultValues: EMPTY_DETAILS_VALUES,
  });

  const engineForm = useForm<EngineFieldsFormValues>({
    resolver: zodResolver(engineFieldsSchema),
    defaultValues: defaultEngineFieldsValues,
  });

  const decodeMutation = useMutation({ mutationFn: (vinToDecode: string) => decodeVin(vinToDecode) });
  const createAssetMutation = useMutation({
    mutationFn: (payload: CreateAssetRequest) => createAsset(householdId, payload),
  });
  const createEngineMutation = useMutation({
    mutationFn: ({ assetId, payload }: { assetId: string; payload: CreateEngineRequest }) =>
      createEngine(householdId, assetId, payload),
  });

  const household = households?.find((item) => item.id === householdId);
  if (!householdsLoading && !canEditWithRole(household?.userRole)) {
    return <Navigate to={`/households/${householdId}/assets`} replace />;
  }

  if (!registry) {
    return null;
  }

  function goToStep(id: WizardStepId) {
    setStepIndex(steps.indexOf(id));
  }

  function handleSelectCategory(nextCategory: AssetCategory) {
    const nextDefinition = findDefinition(registry, nextCategory);
    setCategory(nextCategory);
    if (!nextDefinition) {
      return;
    }
    detailsForm.setValue("usageTrackingMode", nextDefinition.defaultUsageTrackingMode);
    for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
      if (!nextDefinition.applicableFields.includes(key)) {
        detailsForm.setValue(key, "");
      }
    }
    setVin("");
    setDecodeResult(null);
    setDecodeFailed(false);
  }

  function decodeSummary(result: VinDecodeResult | null): string | null {
    if (!result) {
      return null;
    }
    const parts = [result.modelYear ? String(result.modelYear) : null, result.make, result.model].filter(Boolean);
    return parts.length > 0 ? parts.join(" ") : null;
  }

  function handleVinSkip() {
    setDecodeResult(null);
    setDecodeFailed(false);
    goToStep("details");
  }

  function handleVinContinue() {
    if (definition?.applicableFields.includes("vin")) {
      detailsForm.setValue("vin", vin);
    }
    decodeMutation.mutate(vin, {
      onSuccess: (result) => {
        const found = decodeSummary(result) !== null;
        setDecodeResult(found ? result : null);
        setDecodeFailed(!found);
        if (result.make) detailsForm.setValue("make", result.make);
        if (result.model) detailsForm.setValue("model", result.model);
        if (result.modelYear) detailsForm.setValue("year", String(result.modelYear));
        if (result.engineCylinders) engineForm.setValue("cylinders", String(result.engineCylinders));
        if (result.displacementLiters) {
          engineForm.setValue("displacementCc", String(Math.round(result.displacementLiters * 1000)));
        }
        const fuelType = mapFuelTypePrimary(result.fuelTypePrimary);
        if (fuelType) engineForm.setValue("fuelType", fuelType);
        goToStep("details");
      },
      onError: () => {
        setDecodeResult(null);
        setDecodeFailed(true);
        goToStep("details");
      },
    });
  }

  async function createAssetIfNeeded(): Promise<AssetResponse> {
    if (createdAsset) {
      return createdAsset;
    }
    const activeDefinition = definition!;
    const values = detailsForm.getValues();
    const payload: CreateAssetRequest = {
      category: activeDefinition.category,
      name: values.name,
      description: parseOptionalText(values.description),
      year: parseOptionalNumber(values.year),
      usageTrackingMode: values.usageTrackingMode,
      ...clearInapplicableFields(activeDefinition, values),
    };
    const asset = await createAssetMutation.mutateAsync(payload);
    setCreatedAsset(asset);
    return asset;
  }

  async function handleDetailsSubmit() {
    if (steps.includes("engine")) {
      goToStep("engine");
      return;
    }
    try {
      await createAssetIfNeeded();
      goToStep("photos");
    } catch {
      toast.error("Couldn't create this asset. Please try again.");
    }
  }

  function buildIceEnginePayload(values: EngineFieldsFormValues): CreateEngineRequest {
    const torqueFtLbs = parseOptionalNumber(values.torqueFtLbs);
    return {
      label: values.label,
      make: null,
      model: null,
      serialNumber: null,
      year: null,
      engineType: "Ice",
      mechanism: values.mechanism ? values.mechanism : null,
      fuelType: values.fuelType ? values.fuelType : null,
      isExternallyChargeable: null,
      twoStrokeOilDelivery: null,
      twoStrokeMixRatio: null,
      cylinders: parseOptionalNumber(values.cylinders),
      displacementCc: parseOptionalNumber(values.displacementCc),
      installedDate: null,
      installedAtAssetMiles: null,
      installedAtAssetHours: null,
      horsepowerHp: parseOptionalNumber(values.horsepowerHp),
      torqueNm: torqueFtLbs != null ? parseFloat(ftLbsToNm(torqueFtLbs).toFixed(2)) : null,
      oilCapacityL: null,
      recommendedOilType: null,
      coolantCapacityL: null,
      recommendedOctane: null,
    };
  }

  function buildElectricEnginePayload(
    label: string,
    horsepowerHp: string,
    torqueFtLbsInput: string,
    isExternallyChargeable: boolean
  ): CreateEngineRequest {
    const torqueFtLbs = parseOptionalNumber(torqueFtLbsInput);
    return {
      label: label || "Electric motor",
      make: null,
      model: null,
      serialNumber: null,
      year: null,
      engineType: "Electric",
      mechanism: null,
      fuelType: null,
      isExternallyChargeable,
      twoStrokeOilDelivery: null,
      twoStrokeMixRatio: null,
      cylinders: null,
      displacementCc: null,
      installedDate: null,
      installedAtAssetMiles: null,
      installedAtAssetHours: null,
      horsepowerHp: parseOptionalNumber(horsepowerHp),
      torqueNm: torqueFtLbs != null ? parseFloat(ftLbsToNm(torqueFtLbs).toFixed(2)) : null,
      oilCapacityL: null,
      recommendedOilType: null,
      coolantCapacityL: null,
      recommendedOctane: null,
    };
  }

  async function handleEngineSubmit(values: EngineFieldsFormValues) {
    setEngineError(null);
    try {
      const asset = await createAssetIfNeeded();
      try {
        const isHybrid = isHybridChoice(values.wizardEngineType);
        if (isHybrid) {
          if (!createdIceEngineId) {
            const iceEngine = await createEngineMutation.mutateAsync({
              assetId: asset.id,
              payload: buildIceEnginePayload(values),
            });
            setCreatedIceEngineId(iceEngine.id);
          }
          if (!createdElectricEngineId) {
            const electricEngine = await createEngineMutation.mutateAsync({
              assetId: asset.id,
              payload: buildElectricEnginePayload(
                values.electricLabel ?? "",
                values.electricHorsepowerHp,
                values.electricTorqueFtLbs,
                values.wizardEngineType === "Plug-in Hybrid"
              ),
            });
            setCreatedElectricEngineId(electricEngine.id);
          }
        } else if (values.wizardEngineType === "Ice") {
          if (!createdIceEngineId) {
            const engine = await createEngineMutation.mutateAsync({
              assetId: asset.id,
              payload: buildIceEnginePayload(values),
            });
            setCreatedIceEngineId(engine.id);
          }
        } else {
          if (!createdElectricEngineId) {
            const engine = await createEngineMutation.mutateAsync({
              assetId: asset.id,
              payload: buildElectricEnginePayload(values.label, values.horsepowerHp, values.torqueFtLbs, true),
            });
            setCreatedElectricEngineId(engine.id);
          }
        }
        goToStep("photos");
      } catch {
        setEngineError("Couldn't add this engine. The asset was created — you can retry or skip.");
      }
    } catch {
      toast.error("Couldn't create this asset. Please try again.");
    }
  }

  async function handleEngineSkip() {
    setEngineError(null);
    try {
      await createAssetIfNeeded();
      goToStep("photos");
    } catch {
      toast.error("Couldn't create this asset. Please try again.");
    }
  }

  const mismatchHint =
    decodeResult && category
      ? bodyClassMismatchHint(category, decodeResult.bodyClass, decodeResult.vehicleType)
      : null;
  const prefilledFromVin = !!decodeResult;

  return (
    <div className="max-w-2xl space-y-6">
      <h1 className="text-h1">Add asset</h1>
      <WizardStepper labels={steps.map((id) => STEP_LABELS[id])} currentIndex={stepIndex} />

      {currentStep === "type" && (
        <TypeStep
          registry={registry}
          selectedCategory={category}
          onSelect={handleSelectCategory}
          onContinue={() => goToStep(steps.includes("vin") ? "vin" : "details")}
        />
      )}

      {currentStep === "vin" && definition && (
        <VinStep
          vin={vin}
          onVinChange={setVin}
          decodePending={decodeMutation.isPending}
          bestEffort={definition.vinDecodeSupport === "BestEffort"}
          onBack={() => goToStep("type")}
          onSkip={handleVinSkip}
          onContinue={handleVinContinue}
        />
      )}

      {currentStep === "details" && definition && (
        <DetailsStep
          form={detailsForm}
          definition={definition}
          vinDecodeSummary={decodeSummary(decodeResult)}
          vinDecodeFailed={decodeFailed}
          mismatchHint={mismatchHint}
          onBack={() => goToStep(steps.includes("vin") ? "vin" : "type")}
          onSubmit={handleDetailsSubmit}
          submitLabel={steps.includes("engine") ? "Continue" : "Create asset"}
          submitting={createAssetMutation.isPending}
        />
      )}

      {currentStep === "engine" && (
        <EngineStep
          form={engineForm}
          prefilledFromVin={prefilledFromVin}
          errorMessage={engineError}
          submitting={createAssetMutation.isPending || createEngineMutation.isPending}
          onBack={() => goToStep("details")}
          onSkip={handleEngineSkip}
          onSubmit={handleEngineSubmit}
          onRetry={() => engineForm.handleSubmit(handleEngineSubmit)()}
        />
      )}

      {currentStep === "photos" && createdAsset && (
        <PhotosStep
          asset={createdAsset}
          onFinish={() => navigate(`/households/${householdId}/assets/${createdAsset.id}`)}
        />
      )}
    </div>
  );
}
