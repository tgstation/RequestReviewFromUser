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

    if (inputs.ignoreRemovedUsers ?? false)
    {
        timeline = await ghclient.Issue.Timeline.GetAllForIssue(inputs.Owner, inputs.Name, inputs.ID).WaitAsync(TimeSpan.FromSeconds(10));
    }

    for (int i = users.Count - 1; i >= 0; i--)
    {
        string user = users[i];

        //Remove invalid users
        if (!await ghclient.Issue.Assignee.CheckAssignee(inputs.Owner, inputs.Name, user).WaitAsync(TimeSpan.FromSeconds(10)))
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
            IssueEvent removalEvent = await ghclient.Issue.Events.Get(inputs.Owner, inputs.Name, eventInfo.Id).WaitAsync(TimeSpan.FromSeconds(10));
            if (users.Contains(removalEvent.RequestedReviewer.Login))
            {
                Console.WriteLine($"User {removalEvent.RequestedReviewer.Login} will not be request for review since they have previouly been removed.");
                users.Remove(removalEvent.RequestedReviewer.Login);
                continue;
            }
        }
    }

    //Can't request review from the PR author
    PullRequest PR = await ghclient.PullRequest.Get(inputs.Owner, inputs.Name, inputs.ID).WaitAsync(TimeSpan.FromSeconds(10));
    users.Remove(PR.User.Login.ToString());

    Console.WriteLine($"Trying to request review from all valid users: {String.Join(" ", users)}");
    await ghclient.PullRequest.ReviewRequest.Create(inputs.Owner, inputs.Name, inputs.ID, new PullRequestReviewRequest(users, Array.Empty<string>()));

}