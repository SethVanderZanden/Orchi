export const OPEN_BRANCH_REVIEW_EVENT = 'orchi:open-branch-review'

export function requestOpenBranchReview(): void {
  window.dispatchEvent(new Event(OPEN_BRANCH_REVIEW_EVENT))
}
