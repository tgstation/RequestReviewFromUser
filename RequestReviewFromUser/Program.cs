using System;
using CommandLine;
using static CommandLine.Parser;
using RequestReviewFromUser;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Octokit;

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        Console.Write(errors);
        Environment.Exit(2);
    });

parser.WithParsed(options => RequestReviewFromUser(options));

static void RequestReviewFromUser(ActionInputs inputs)
{
    Console.WriteLine($"RequestReviewFromUser Version {System.Reflection.Assembly.GetEntryAssembly().GetName().Version}");

    List<string> users = new List<string>();
    List<string> nonAssignableUsers = new List<string>();

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

    users
    .Where(user => !ghclient.Issue.Assignee.CheckAssignee(inputs.Owner, inputs.Name, user).Result)
    .ToList()
    .ForEach(user =>
    {
        Console.WriteLine($"User {user} cannot be requested for review, make sure he is member of a team with read access .");
        users.Remove(user);
    }
    );

    //Can't request review from the PR author
    PullRequest PR = ghclient.PullRequest.Get(inputs.Owner, inputs.Name, inputs.ID).Result;
    users.Remove(PR.User.Login.ToString());

    Console.WriteLine($"Trying to request review from all valid users: {String.Join(" ", users)}");
    var result = ghclient.PullRequest.ReviewRequest.Create(inputs.Owner, inputs.Name, inputs.ID, new PullRequestReviewRequest(users, Array.Empty<string>())).Result;

}