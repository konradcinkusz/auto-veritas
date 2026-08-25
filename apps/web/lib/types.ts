// Mirrors of the OffersService contracts (src/AutoVeritas.OffersService/Contracts).
// The server is authoritative; these exist only to type the JSON it already sent.

export type FreshnessStatus = 'Fresh' | 'Warning' | 'Stale' | 'Expired';
export type Confidence = 'Confirmed' | 'Estimated';
export type DgtLabel = 'Cero' | 'Eco' | 'C' | 'B';
export type FinancingType = 'Bank' | 'Green' | 'Fintech' | 'Manufacturer' | 'Subscription';
export type RepaymentStructure = 'Linear' | 'Balloon' | 'Subscription' | 'Unknown';

export interface CarOffer {
  id: string;
  slug: string;
  name: string;
  variant: string;
  dgtLabel: DgtLabel;
  powerCv: number;
  cashPriceEur: number | null;
  financedPriceEur: number | null;
  priceGapEur: number | null;
  reliabilityScore: number | null;
  reliabilityText: string | null;
  bootLiters: number | null;
  notes: string | null;
  priceConfidence: Confidence;
  sourceName: string | null;
  sourceUrl: string | null;
  lastVerifiedAt: string;
  offerValidUntil: string | null;
  sourcePublishedAt: string | null;
  daysSinceVerification: number;
  priceFreshness: FreshnessStatus;
  specFreshness: FreshnessStatus;
  isExpired: boolean;
}

export interface FinancingOffer {
  id: string;
  slug: string;
  provider: string;
  type: FinancingType;
  tinPercent: number | null;
  taePercent: number | null;
  repaymentStructure: RepaymentStructure;
  termDescription: string;
  downPaymentDescription: string;
  feesDescription: string;
  monthlyInstallment60Eur: number | null;
  totalInterest60Eur: number | null;
  bestFor: string;
  rateConfidence: Confidence;
  sourceName: string | null;
  sourceUrl: string | null;
  lastVerifiedAt: string;
  offerValidUntil: string | null;
  sourcePublishedAt: string | null;
  daysSinceVerification: number;
  rateFreshness: FreshnessStatus;
  isExpired: boolean;
}

export interface CarOfferHistoryEntry {
  id: string;
  recordedAt: string;
  changedByEmail: string | null;
  cashPriceEur: number | null;
  financedPriceEur: number | null;
  priceConfidence: Confidence;
  lastVerifiedAt: string;
  offerValidUntil: string | null;
}

export interface FinancingOfferHistoryEntry {
  id: string;
  recordedAt: string;
  changedByEmail: string | null;
  tinPercent: number | null;
  taePercent: number | null;
  repaymentStructure: RepaymentStructure;
  monthlyInstallment60Eur: number | null;
  rateConfidence: Confidence;
  lastVerifiedAt: string;
  offerValidUntil: string | null;
}

export interface ListResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  limit: number;
}

export interface FreshnessPolicy {
  priceFreshDays: number;
  priceWarningDays: number;
  rateFreshDays: number;
  rateWarningDays: number;
  specFreshDays: number;
  specWarningDays: number;
}

export interface Session {
  authenticated: boolean;
  email?: string | null;
  roles?: string[];
}
