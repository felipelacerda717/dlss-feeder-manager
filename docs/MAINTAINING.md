# Maintainer safety checklist

## Protect the main branch

- [ ] Open Settings > Rules > Rulesets.
- [ ] Create a branch ruleset named `Protect main`.
- [ ] Set enforcement to Active.
- [ ] Target the default branch.
- [ ] Enable Restrict deletions.
- [ ] Enable Block force pushes.
- [ ] Require a pull request before merging.
- [ ] Keep required approvals at zero while the repository has one maintainer.
- [ ] Require status checks to pass.
- [ ] Add the `build` status check.
- [ ] Require the branch to be up to date before merging.
- [ ] Save the ruleset and confirm that `main` is shown as protected.

## Repository access

- [ ] Keep Admin access limited to the repository owner.
- [ ] Give Write access only to trusted maintainers.
- [ ] Review installed GitHub Apps and remove unused access.
- [ ] Keep the default Actions workflow permission read-only.
- [ ] Grant write permission only to the individual workflow job that needs it.
- [ ] Never commit tokens, private NVIDIA files, game files, or restricted third-party binaries.

## Merge a change

- [ ] Create a dedicated branch.
- [ ] Open a pull request.
- [ ] Confirm that `build` passes.
- [ ] Review changed filenames and the complete diff.
- [ ] Confirm that no restricted binary was added.
- [ ] Squash merge into `main`.
- [ ] Confirm that the main-branch build passes.

## Publish a release

- [ ] Update the application version.
- [ ] Build from the protected `main` branch.
- [ ] Verify the Windows executable SHA-256.
- [ ] Publish the executable and checksum in the same GitHub Release.
- [ ] Record third-party component versions used for verified profiles.
- [ ] Keep iMMERSE, RenoDX, NVIDIA runtimes, ReShade, and game files out of the release.
