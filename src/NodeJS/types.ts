/** Custom type to mirror .NET timespan consumable for JS date libraries like DateFns. */
export interface TimeSpan {
  days?: number;
  hours?: number;
  minutes?: number;
  seconds?: number;
}
