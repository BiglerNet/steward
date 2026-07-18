export type HouseholdMemberRole = "Owner" | "Contributor" | "Viewer";
export type HouseholdMemberStatus = "Pending" | "Active" | "Revoked";
export type InvitationStatus = "Pending" | "Accepted" | "Revoked" | "Expired";
export type ThemePreference = "Light" | "Dark" | "System";

export interface PendingInviteSummary {
  inviteCode: string;
  householdName: string;
  role: HouseholdMemberRole;
  expiresAt: string;
}

export interface AuthenticatedUser {
  id: string;
  email: string;
  displayName: string | null;
  themePreference: ThemePreference | null;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
  pendingInvites: PendingInviteSummary[];
}

export interface UserProfileResponse {
  id: string;
  email: string;
  displayName: string | null;
  avatarUrl: string | null;
  themePreference: ThemePreference | null;
}

export interface UpdateThemePreferenceRequest {
  themePreference: ThemePreference;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface OAuthExchangeRequest {
  code: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface OAuthProvidersResponse {
  google: boolean;
  facebook: boolean;
  apple: boolean;
}

export interface HouseholdResponse {
  id: string;
  name: string;
  publicSlug: string;
  isPublicVisible: boolean;
  country: string | null;
  region: string | null;
  userRole: HouseholdMemberRole;
  storageUsedBytes: number;
  storageQuotaBytes: number;
  createdAt: string;
}

export interface CreateHouseholdRequest {
  name: string;
  publicSlug: string;
  isPublicVisible: boolean;
  country: string | null;
  region: string | null;
}

export interface UpdateHouseholdRequest {
  name: string;
  publicSlug: string;
  isPublicVisible: boolean;
  country: string | null;
  region: string | null;
}

export interface RegionDefinition {
  code: string;
  name: string;
}

export interface CountryDefinition {
  code: string;
  name: string;
  regions: RegionDefinition[];
}

export interface MembershipResponse {
  userId: string;
  displayName: string | null;
  email: string;
  role: HouseholdMemberRole;
  status: HouseholdMemberStatus;
}

export interface InvitationResponse {
  id: string;
  email: string;
  role: HouseholdMemberRole;
  inviteCode: string;
  expiresAt: string;
  status: InvitationStatus;
}

export interface HouseholdMembersResponse {
  members: MembershipResponse[];
  pendingInvites: InvitationResponse[];
}

export interface InviteMemberRequest {
  email: string;
  role: HouseholdMemberRole;
}

export type AssetCategory =
  | "Car"
  | "Truck"
  | "Suv"
  | "Van"
  | "Motorcycle"
  | "Utv"
  | "Atv"
  | "Snowmobile"
  | "DirtBike"
  | "GolfCart"
  | "PowerBoat"
  | "Sailboat"
  | "Pwc"
  | "UtilityTrailer"
  | "EnclosedTrailer"
  | "SnowmobileTrailer"
  | "BoatTrailer"
  | "RidingMower"
  | "PowerWasher"
  | "Generator"
  | "SmallEngine";

export type AssetGroup = "Road" | "Powersport" | "Water" | "Trailer" | "Equipment";

export type AssetStructuralType = "Vehicle" | "Boat" | "Trailer" | "Equipment";

export type VinDecodeSupport = "None" | "BestEffort" | "Supported";

export type HullType = "Monohull" | "Catamaran" | "Trimaran" | "Pontoon" | "Other";
export type DriveType = "Inboard" | "Outboard" | "SternDrive" | "JetDrive";

export type UsageTrackingMode = "None" | "Mileage" | "Hours" | "Both";

export interface VinDecodeResult {
  vin: string;
  make: string | null;
  model: string | null;
  modelYear: number | null;
  bodyClass: string | null;
  vehicleType: string | null;
  fuelTypePrimary: string | null;
  engineCylinders: number | null;
  displacementLiters: number | null;
}

export interface AssetTypeDefinition {
  category: AssetCategory;
  group: AssetGroup;
  structuralType: AssetStructuralType;
  displayLabel: string;
  defaultUsageTrackingMode: UsageTrackingMode;
  typicallyHasEngine: boolean;
  vinDecodeSupport: VinDecodeSupport;
  typicalPermitKinds: string[];
  applicableFields: string[];
  icon: string;
}

export interface AssetResponse {
  id: string;
  householdId: string;
  category: AssetCategory;
  structuralType: AssetStructuralType;
  name: string;
  description: string | null;
  year: number | null;
  coverPhotoId: string | null;
  usageTrackingMode: UsageTrackingMode;
  vin: string | null;
  make: string | null;
  model: string | null;
  color: string | null;
  trackLengthIn: number | null;
  hin: string | null;
  hullMaterial: string | null;
  hullType: HullType | null;
  driveType: DriveType | null;
  keelType: string | null;
  mastHeightFt: number | null;
  mastCount: number | null;
  lengthFt: number | null;
  beamFt: number | null;
  ballSizeIn: number | null;
  maxLoadLbs: number | null;
  interiorHeightFt: number | null;
  interiorLengthFt: number | null;
  cuttingWidthIn: number | null;
  maxPsi: number | null;
  maxGpm: number | null;
  equipmentDescription: string | null;
  licensePlate: string | null;
  createdAt: string;
  updatedAt: string;
  powertrain: Powertrain | null;
}

export type Powertrain = "Electric" | "Hybrid" | "Plug-in Hybrid";

export type AssetFields = Omit<
  AssetResponse,
  "id" | "householdId" | "structuralType" | "coverPhotoId" | "createdAt" | "updatedAt" | "powertrain"
>;

export type CreateAssetRequest = Omit<AssetFields, "usageTrackingMode"> & {
  usageTrackingMode: UsageTrackingMode | null;
};
export type UpdateAssetRequest = AssetFields;

export interface AssetPhotoResponse {
  id: string;
  assetId: string;
  width: number;
  height: number;
  sizeBytes: number;
  createdAt: string;
}

export interface SetCoverPhotoRequest {
  photoId: string;
}

export type PhotoVariant = "thumb" | "display";

export type EngineType = "Ice" | "Electric";
export type Mechanism = "TwoStroke" | "FourStroke" | "Diesel" | "Rotary";
export type FuelType = "Gasoline" | "Diesel" | "Propane";
export type TwoStrokeOilDelivery = "Premix" | "OilInjected";
export type EngineStatus = "Active" | "Retired" | "Broken";

export interface EngineResponse {
  id: string;
  assetId: string;
  label: string;
  make: string | null;
  model: string | null;
  serialNumber: string | null;
  year: number | null;
  engineType: EngineType;
  mechanism: Mechanism | null;
  fuelType: FuelType | null;
  isExternallyChargeable: boolean | null;
  twoStrokeOilDelivery: TwoStrokeOilDelivery | null;
  twoStrokeMixRatio: string | null;
  cylinders: number | null;
  displacementCc: number | null;
  status: EngineStatus;
  installedDate: string | null;
  installedAtAssetMiles: number | null;
  installedAtAssetHours: number | null;
  horsepowerHp: number | null;
  torqueNm: number | null;
  oilCapacityL: number | null;
  recommendedOilType: string | null;
  coolantCapacityL: number | null;
  recommendedOctane: number | null;
}

export interface CreateEngineRequest {
  label: string;
  make: string | null;
  model: string | null;
  serialNumber: string | null;
  year: number | null;
  engineType: EngineType;
  mechanism: Mechanism | null;
  fuelType: FuelType | null;
  isExternallyChargeable: boolean | null;
  twoStrokeOilDelivery: TwoStrokeOilDelivery | null;
  twoStrokeMixRatio: string | null;
  cylinders: number | null;
  displacementCc: number | null;
  installedDate: string | null;
  installedAtAssetMiles: number | null;
  installedAtAssetHours: number | null;
  horsepowerHp: number | null;
  torqueNm: number | null;
  oilCapacityL: number | null;
  recommendedOilType: string | null;
  coolantCapacityL: number | null;
  recommendedOctane: number | null;
}

export type UpdateEngineRequest = CreateEngineRequest;

export interface ServiceRecordResponse {
  id: string;
  assetId: string;
  engineId: string | null;
  date: string;
  description: string;
  providerName: string | null;
  cost: number | null;
  odometerMiles: number | null;
  engineHours: number | null;
  notes: string | null;
}

export interface CreateServiceRecordRequest {
  date: string;
  description: string;
  providerName: string | null;
  cost: number | null;
  odometerMiles: number | null;
  engineHours: number | null;
  engineId: string | null;
  notes: string | null;
}

export type UpdateServiceRecordRequest = CreateServiceRecordRequest;

export interface MileageLogResponse {
  id: string;
  assetId: string;
  date: string;
  odometerReading: number | null;
  tripMiles: number | null;
  notes: string | null;
}

export interface CreateMileageLogRequest {
  date: string;
  odometerReading: number | null;
  tripMiles: number | null;
  notes: string | null;
}

export type UpdateMileageLogRequest = CreateMileageLogRequest;

export interface EngineHoursLogResponse {
  id: string;
  engineId: string;
  date: string;
  hoursReading: number | null;
  tripHours: number | null;
  notes: string | null;
}

export interface CreateEngineHoursLogRequest {
  date: string;
  hoursReading: number | null;
  tripHours: number | null;
  notes: string | null;
}

export type UpdateEngineHoursLogRequest = CreateEngineHoursLogRequest;

export type FuelLogType = "Fillup" | "Consumption";
export type VolumeUnit = "Gallons" | "Liters" | "Kwh";

export interface FuelLogResponse {
  id: string;
  assetId: string;
  engineId: string | null;
  logType: FuelLogType;
  date: string;
  quantity: number;
  unit: VolumeUnit;
  fuelGrade: string | null;
  pricePerUnit: number | null;
  totalCost: number | null;
  milesAtLog: number | null;
  hoursAtLog: number | null;
  notes: string | null;
}

export interface CreateFuelLogRequest {
  logType: FuelLogType;
  date: string;
  quantity: number;
  unit: VolumeUnit;
  fuelGrade: string | null;
  pricePerUnit: number | null;
  totalCost: number | null;
  milesAtLog: number | null;
  hoursAtLog: number | null;
  engineId: string | null;
  notes: string | null;
}

export type UpdateFuelLogRequest = CreateFuelLogRequest;

export type RegistrationKind = "Registration" | "TrailPass" | "Permit";

export interface RegistrationResponse {
  id: string;
  assetId: string;
  kind: RegistrationKind;
  registrationNumber: string | null;
  issuingAuthority: string | null;
  validFrom: string | null;
  renewedOn: string | null;
  cost: number | null;
  expiresOn: string | null;
  notes: string | null;
  hasDocument: boolean;
  documentUrl: string | null;
}

export interface CreateRegistrationRequest {
  kind: RegistrationKind;
  registrationNumber: string | null;
  issuingAuthority: string | null;
  validFrom: string | null;
  renewedOn: string | null;
  cost: number | null;
  expiresOn: string | null;
  notes: string | null;
}

export type UpdateRegistrationRequest = CreateRegistrationRequest;

export interface WarrantyResponse {
  id: string;
  assetId: string;
  provider: string;
  description: string | null;
  startsOn: string | null;
  expiresOn: string | null;
  notes: string | null;
  hasDocument: boolean;
  documentUrl: string | null;
}

export interface CreateWarrantyRequest {
  provider: string;
  description: string | null;
  startsOn: string | null;
  expiresOn: string | null;
  notes: string | null;
}

export type UpdateWarrantyRequest = CreateWarrantyRequest;

export type WidgetType =
  | "AssetCount"
  | "CylinderIndex"
  | "TotalDisplacement"
  | "TotalHorsepower"
  | "TotalTorque"
  | "DueSoon"
  | "RecentActivity"
  | "FuelCostYtd"
  | "MileageMtd";

export type WidgetSize = "Small" | "Wide" | "Full";

export interface DashboardSummaryResponse {
  id: string;
  name: string;
  isDefault: boolean;
  position: number;
}

export interface WidgetResponse {
  id: string;
  widgetType: WidgetType;
  widgetSize: WidgetSize;
  position: number;
  config: string | null;
}

export interface DashboardDetailResponse {
  id: string;
  name: string;
  isDefault: boolean;
  position: number;
  widgets: WidgetResponse[];
}

export interface CreateDashboardRequest {
  name: string;
  isDefault?: boolean;
}

export interface UpdateDashboardRequest {
  name: string;
  isDefault: boolean;
  position: number;
}

export interface WidgetDefinition {
  widgetType: WidgetType;
  widgetSize: WidgetSize;
  config?: string | null;
}

export interface ReplaceWidgetLayoutRequest {
  widgets: WidgetDefinition[];
}

export interface AssetCountData { count: number }
export interface CylinderIndexData { totalCylinders: number; engineCount: number }
export interface TotalDisplacementData { totalCc: number; engineCount: number }
export interface TotalHorsepowerData { totalHp: number; engineCount: number }
export interface TotalTorqueData { totalNm: number; engineCount: number }

export interface DueItem {
  assetId: string;
  assetName: string;
  recordType: "Registration" | "Warranty" | "MaintenanceRecurrence";
  expiresOn: string | null;
  urgency: "Overdue" | "DueSoon" | "Upcoming";
  stepText?: string | null;
  engineLabel?: string | null;
}
export interface DueSoonData { items: DueItem[] }

export interface ActivityItem {
  assetId: string;
  assetName: string;
  description: string;
  performedOn: string;
  cost: number | null;
}
export interface RecentActivityData { items: ActivityItem[] }

export interface FuelCostYtdData { totalCost: number; logCount: number }
export interface MileageMtdData { totalMiles: number; logCount: number }

export type WidgetData =
  | AssetCountData
  | CylinderIndexData
  | TotalDisplacementData
  | TotalHorsepowerData
  | TotalTorqueData
  | DueSoonData
  | RecentActivityData
  | FuelCostYtdData
  | MileageMtdData;

export type DashboardSnapshot = Partial<{
  AssetCount: AssetCountData;
  CylinderIndex: CylinderIndexData;
  TotalDisplacement: TotalDisplacementData;
  TotalHorsepower: TotalHorsepowerData;
  TotalTorque: TotalTorqueData;
  DueSoon: DueSoonData;
  RecentActivity: RecentActivityData;
  FuelCostYtd: FuelCostYtdData;
  MileageMtd: MileageMtdData;
}>;

export type MaintenanceItemStatus = "Planned" | "InProgress" | "Done" | "Cancelled";
export type ChecklistItemStatus = "Open" | "Done" | "Skipped";
export type PartLineStatus = "Needed" | "Ordered" | "Received";

export interface ChecklistItemResponse {
  id: string;
  maintenanceItemId: string;
  text: string;
  status: ChecklistItemStatus;
  resolvedAt: string | null;
  sortOrder: number;
  engineId: string | null;
  templateStepId: string | null;
}

export interface CreateChecklistItemRequest {
  text: string;
  engineId?: string | null;
}

export interface PatchChecklistItemRequest {
  text?: string;
  status?: ChecklistItemStatus;
  engineId?: string | null;
}

export interface PartLineResponse {
  id: string;
  maintenanceItemId: string;
  name: string;
  partNumber: string | null;
  vendor: string | null;
  trackingNumber: string | null;
  orderUrl: string | null;
  quantity: number;
  status: PartLineStatus;
  cost: number | null;
  checklistItemId: string | null;
  partId: string | null;
}

export interface CreatePartLineRequest {
  name: string;
  partNumber?: string | null;
  vendor?: string | null;
  trackingNumber?: string | null;
  orderUrl?: string | null;
  quantity?: number;
  cost?: number | null;
  checklistItemId?: string | null;
}

export interface PatchPartLineRequest {
  name?: string;
  partNumber?: string | null;
  vendor?: string | null;
  trackingNumber?: string | null;
  orderUrl?: string | null;
  quantity?: number;
  status?: PartLineStatus;
  cost?: number | null;
  checklistItemId?: string | null;
}

export interface MaintenanceItemResponse {
  id: string;
  assetId: string;
  engineId: string | null;
  templateId: string | null;
  title: string;
  description: string | null;
  providerName: string | null;
  status: MaintenanceItemStatus;
  date: string | null;
  cost: number | null;
  odometerMiles: number | null;
  engineHours: number | null;
  isBlocked: boolean;
  completedAt: string | null;
  checklistItems: ChecklistItemResponse[];
  partLines: PartLineResponse[];
}

export interface CreateMaintenanceItemRequest {
  title: string;
  description?: string | null;
  providerName?: string | null;
  status?: MaintenanceItemStatus;
  date?: string | null;
  cost?: number | null;
  odometerMiles?: number | null;
  engineHours?: number | null;
  engineId?: string | null;
  templateId?: string | null;
}

export interface PatchMaintenanceItemRequest {
  title?: string;
  description?: string | null;
  providerName?: string | null;
  status?: MaintenanceItemStatus;
  date?: string | null;
  cost?: number | null;
  odometerMiles?: number | null;
  engineHours?: number | null;
  engineId?: string | null;
}

export interface HouseholdMaintenanceItemResponse {
  id: string;
  assetId: string;
  assetName: string;
  engineId: string | null;
  templateId: string | null;
  title: string;
  description: string | null;
  providerName: string | null;
  status: MaintenanceItemStatus;
  date: string | null;
  cost: number | null;
  odometerMiles: number | null;
  engineHours: number | null;
  isBlocked: boolean;
  completedAt: string | null;
  checklistItems: ChecklistItemResponse[];
  partLines: PartLineResponse[];
}

export type MaintenanceDueStatus = "Overdue" | "DueSoon" | "Upcoming" | "OK" | "Unknown";
export type ReadingUnit = "Miles" | "Hours";

export interface MaintenanceReadingResponse {
  value: number;
  unit: ReadingUnit;
}

export interface MaintenanceScheduleEntryResponse {
  templateId: string;
  templateTitle: string;
  templateStepId: string;
  stepText: string;
  engineId: string | null;
  engineLabel: string | null;
  lastDoneAt: string | null;
  lastDoneReading: MaintenanceReadingResponse | null;
  intervalMonths: number | null;
  intervalMiles: number | null;
  intervalHours: number | null;
  dueStatus: MaintenanceDueStatus;
}

export interface SuggestedPartDto {
  name: string;
  quantity: number;
}

export interface TemplateStepResponse {
  id: string;
  templateId: string;
  text: string;
  sortOrder: number;
  engineScoped: boolean;
  recurrenceIntervalMonths: number | null;
  recurrenceIntervalMiles: number | null;
  recurrenceIntervalHours: number | null;
  suggestedParts: SuggestedPartDto[];
}

export interface TemplateResponse {
  id: string;
  householdId: string | null;
  title: string;
  description: string | null;
  applicableCategories: AssetCategory[];
  steps: TemplateStepResponse[];
}

export interface CreateTemplateRequest {
  title: string;
  description?: string | null;
  applicableCategories?: AssetCategory[];
}

export interface PatchTemplateRequest {
  title?: string;
  description?: string | null;
  applicableCategories?: AssetCategory[];
}

export interface CreateTemplateStepRequest {
  text: string;
  engineScoped?: boolean;
  recurrenceIntervalMonths?: number | null;
  recurrenceIntervalMiles?: number | null;
  recurrenceIntervalHours?: number | null;
  suggestedParts?: SuggestedPartDto[];
}

export interface PatchTemplateStepRequest {
  text?: string;
  engineScoped?: boolean;
  recurrenceIntervalMonths?: number | null;
  recurrenceIntervalMiles?: number | null;
  recurrenceIntervalHours?: number | null;
  suggestedParts?: SuggestedPartDto[];
}

export interface DuplicateTemplateRequest {
  platformTemplateId: string;
}

export interface ProblemDetailsResponse {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
