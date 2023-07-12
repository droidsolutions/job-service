import CancellationToken from "cancellationtoken";

/**
 * Checks if the given object is a CancellationError.
 * @param {any} err Any error.
 * @returns {boolean} true if the given object is a CancellationError.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isCancellationError = (err: any): err is CancellationToken.CancellationError => {
  return err instanceof CancellationToken.CancellationError;
};
