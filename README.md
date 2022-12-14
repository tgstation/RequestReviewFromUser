# RequestReviewFromUser
## Github action for assigning a list of users to a PR as reviewers
RequestReviewFromUser is an Github action that takes a list of users and assigns them as reviewer to the specified PR.
The specified users must be collaborators of the repository. 

### Inputs
RequestReviewFromUser takes the following inputs:
Name | Required | Default | Description
------------ | ------------- | ------------- | -------------
owner | no | github.repository_owner | Name of repo owner
name | no | github.repository | Name of the repo
token | no | github.token | Token used for REST calls. Only needed to increase rate limits, can be replaced with empty string, but might lead to rate limit errors.
ID | no | github.event.pull_request.number | ID of the PR to get modified files from
separator | no | null | Separator used when multiple users are provided.
user | no | null | User(s) to request review from. Separated by the specified separator if multiple.
ignoreRemovedUsers | no | false | Don't request review from users that got got previously removed as reviewer for the PR.
