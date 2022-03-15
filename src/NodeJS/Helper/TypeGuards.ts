import CancellationToken from "cancellationtoken";

/**
 * Checks if the given object is a CancellationError.
 *
 * @param err Any error.
 * @returns true if the given object is a CancellationError.
 */
export const isCancellationError = (err: any): err is CancellationToken.CancellationError => {
  return err instanceof CancellationToken.CancellationError;
};
