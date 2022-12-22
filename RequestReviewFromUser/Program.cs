using System;
using CommandLine;
using static CommandLine.Parser;
using RequestReviewFromUser;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Octokit;
using System.Threading.Tasks;

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        Console.Write(errors);
        Environment.Exit(2);
    });

parser.WithParsed(options => RequestReviewFromUser(options).Wait(TimeSpan.FromSeconds(100)));

static async Task RequestReviewFromUser(ActionInputs inputs)
{
    Console.WriteLine($"RequestReviewFromUser Version {System.Reflection.Assembly.GetEntryAssembly().GetName().Version}");

    List<string> users = new List<string>();
    List<string> nonAssignableUsers = new List<string>();
    IReadOnlyList<TimelineEventInfo> timeline = new List<TimelineEventInfo>();

    //Split input and remove leading @
    if (inputs.separator is null)
    {
        users.Add(inputs.users.TrimStart('@'));
    }
    else
    {
        users.AddRange(inputs.users.Split(inputs.separator).Select(user => user.TrimStart('@')));
    }

    GitHubClient ghclient = new GitHubClient(new ProductHeaderValue("RequestReviewFromUser"));
    ghclient.Credentials = new Credentials(inputs.token);

    var batchPagination = new ApiOptions
    {
        PageSize = 100
    };
    if (!inputs.alwaysRequestAll ?? true)
    {
        timeline = await ghclient.Issue.Timeline.GetAllForIssue(inputs.Owner, inputs.Name, inputs.ID, batchPagination);
    }


    //Can't request review from the PR author
    PullRequest PR = await ghclient.PullRequest.Get(inputs.Owner, inputs.Name, inputs.ID);
    users.Remove(PR.User.Login.ToString());

    //Remove invalid users
    for (int i = users.Count - 1; i >= 0; i--)
    {
        string user = users[i];

        if (!await ghclient.Issue.Assignee.CheckAssignee(inputs.Owner, inputs.Name, user))
        {
            Console.WriteLine($"User {user} cannot be requested for review, make sure they are a member of a team with read access.");
            users.RemoveAt(i);
            continue;
        }
    }

    //Remove previously removed users
    foreach (TimelineEventInfo eventInfo in timeline)
    {
        eventInfo.Event.TryParse(out EventInfoState eventState);
        if (eventState == EventInfoState.ReviewRequestRemoved)
        {
            IssueEvent removalEvent = await ghclient.Issue.Events.Get(inputs.Owner, inputs.Name, eventInfo.Id);
            //Remove users that removed themselves from review
            if (users.Contains(removalEvent.RequestedReviewer.Login) && removalEvent.Actor.Id == removalEvent.RequestedReviewer.Id)
            {
                Console.WriteLine($"User {removalEvent.RequestedReviewer.Login} will not be request for review because they previouly removed themselves from review.");
                users.Remove(removalEvent.RequestedReviewer.Login);
                continue;
            }
            //Otherwise check if remover(actor) is maintainer (write or admin perms)
            else
            {
                CollaboratorPermission actorPerms = await ghclient.Repository.Collaborator.ReviewPermission(inputs.Owner, inputs.Name, removalEvent.Actor.Login);
                actorPerms.Permission.TryParse(out PermissionLevel permLevel);
                if (permLevel == PermissionLevel.Admin || permLevel == PermissionLevel.Write)
                {
                    Console.WriteLine($"User {removalEvent.RequestedReviewer.Login} will not be request for review because a maintainer ({removalEvent.Actor.Login}) previouly removed them from review.");
                    users.Remove(removalEvent.RequestedReviewer.Login);
                    continue;
                }
            }
        }
    }


    Console.WriteLine($"Trying to request review from all valid users: {String.Join(" ", users)}");
    await ghclient.PullRequest.ReviewRequest.Create(inputs.Owner, inputs.Name, inputs.ID, new PullRequestReviewRequest(users, Array.Empty<string>()));

}