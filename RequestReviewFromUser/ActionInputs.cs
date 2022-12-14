using System;
using CommandLine;

namespace RequestReviewFromUser
{
    public class ActionInputs
    {
        string _repositoryName = null!;

        public ActionInputs()
        {

        }

        [Option('o', "owner",
            Required = true,
            HelpText = "The owner, for example: \"dotnet\". Assign from `github.repository_owner`.")]
        public string Owner { get; set; } = null!;

        [Option('n', "name",
            Required = true,
            HelpText = "The repository name, for example: \"samples\". Assign from `github.repository`.")]
        public string Name
        {
            get => _repositoryName;
            set => ParseAndAssign(value, str => _repositoryName = str);
        }

        [Option('t', "token",
           Required = true,
           HelpText = "Github token. Assign from `github.token`.")]
        public string token { get; set; } = null!;

        [Option('i', "ID",
           Required = true,
           HelpText = "ID of the PR. Assign from `github.event_path.pull_request.number`.")]
        public int ID { get; set; } = 0;

        [Option('s', "separator",
           Required = false,
           HelpText = "When providing multiple users, characters used to separate input.")]
        public string separator { get; set; } = null!;

        [Option('u', "users",
           Required = true,
           HelpText = "Single or multiple users to assign.")]
        public string users { get; set; } = null!;

        [Option('r', "ignoreRemovedUsers",
           Required = false,
           HelpText = "Don't request review from users that got got previously removed as reviewer for the PR.")]
        public bool? ignoreRemovedUsers { get; set; } = false;

        static void ParseAndAssign(string? value, Action<string> assign)
        {
            if (value is { Length: > 0 } && assign is not null)
            {
                assign(value.Split("/")[^1]);
            }
        }
    }
}
