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
}

export interface OAuthExchangeRequest {
  code: string;
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
  userRole: HouseholdMemberRole;
  createdAt: string;
}

export interface CreateHouseholdRequest {
  name: string;
  publicSlug: string;
  isPublicVisible: boolean;
}

export interface UpdateHouseholdRequest {
  name: string;
  publicSlug: string;
  isPublicVisible: boolean;
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

export type AssetType =
  | "Snowmobile"
  | "Utv"
  | "Boat"
  | "Car"
  | "Truck"
  | "SnowmobileTrailer"
  | "EnclosedTrailer"
  | "RidingMower"
  | "PowerWasher"
  | "SmallEngine";

export type UsageTrackingMode = "None" | "Mileage" | "Hours" | "Both";

export interface AssetResponse {
  id: string;
  householdId: string;
  assetType: AssetType;
  name: string;
  description: string | null;
  year: number | null;
  photoUrl: string | null;
  usageTrackingMode: UsageTrackingMode;
  vin: string | null;
  color: string | null;
  make: string | null;
  model: string | null;
  hin: string | null;
  hullMaterial: string | null;
  lengthFt: number | null;
  beamFt: number | null;
  trackLengthIn: number | null;
  ballSizeIn: number | null;
  maxLoadLbs: number | null;
  interiorHeightFt: number | null;
  interiorLengthFt: number | null;
  cuttingWidthIn: number | null;
  maxPsi: number | null;
  maxGpm: number | null;
  equipmentDescription: string | null;
  createdAt: string;
  updatedAt: string;
}

export type AssetFields = Omit<
  AssetResponse,
  "id" | "householdId" | "createdAt" | "updatedAt"
>;

export type CreateAssetRequest = AssetFields;
export type UpdateAssetRequest = AssetFields;

export type EngineType = "Ice" | "Electric" | "Hybrid";
export type FuelType = "Gasoline" | "Diesel" | "TwoStroke" | "FourStroke" | "Electric" | "None";
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
  fuelType: FuelType;
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
  fuelType: FuelType;
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
export type VolumeUnit = "Gallons" | "Liters";

export interface FuelLogResponse {
  id: string;
  assetId: string;
  engineId: string | null;
  logType: FuelLogType;
  date: string;
  volume: number;
  volumeUnit: VolumeUnit;
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
  volume: number;
  volumeUnit: VolumeUnit;
  fuelGrade: string | null;
  pricePerUnit: number | null;
  totalCost: number | null;
  milesAtLog: number | null;
  hoursAtLog: number | null;
  engineId: string | null;
  notes: string | null;
}

export type UpdateFuelLogRequest = CreateFuelLogRequest;

export interface RegistrationResponse {
  id: string;
  assetId: string;
  registrationNumber: string;
  issuingAuthority: string | null;
  renewedOn: string | null;
  cost: number | null;
  expiresOn: string | null;
  notes: string | null;
  hasDocument: boolean;
  documentUrl: string | null;
}

export interface CreateRegistrationRequest {
  registrationNumber: string;
  issuingAuthority: string | null;
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
  recordType: "Registration" | "Warranty";
  expiresOn: string;
  urgency: "Overdue" | "DueSoon" | "Upcoming";
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

export interface ProblemDetailsResponse {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
