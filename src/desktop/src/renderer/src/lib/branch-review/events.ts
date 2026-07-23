export const OPEN_BRANCH_REVIEW_EVENT = 'orchi:open-branch-review'

export type OpenBranchReviewDetail = {
  projectId?: string | null
}

export function requestOpenBranchReview(detail?: OpenBranchReviewDetail): void {
  window.dispatchEvent(
    new CustomEvent<OpenBranchReviewDetail>(OPEN_BRANCH_REVIEW_EVENT, {
      detail: detail ?? {}
    })
  )
}
