import { addMinutes } from "date-fns";

/**
 * Gets the given date in UTC.
 * @param {Date} date The date.
 * @returns {Date} The current date in UTC.
 * @deprecated Use `new Date()` instead
 */
export const transformDateToUtc = (date: Date = new Date()): Date => {
  return addMinutes(date, date.getTimezoneOffset());
};
