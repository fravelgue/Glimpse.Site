﻿using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

namespace Glimpse.Release
{
    public class ReleaseService : IReleaseService
    {
        private readonly IMilestoneProvider _milestoneProvider;
        private readonly IIssueProvider _issueProvider;
        private readonly IPackageProvider _packageProvider;
        private const string NextMilestone = "vNext";

        public ReleaseService(IMilestoneProvider milestoneProvider, IIssueProvider issueProvider, IPackageProvider packageProvider)
        {
            _milestoneProvider = milestoneProvider;
            _issueProvider = issueProvider;
            _packageProvider = packageProvider;
        }

        public Release GetRelease(string milestoneNumber)
        {
            var milestone = (GithubMilestone)null;
            var issues = (IList<GithubIssue>)null;

            // Fetch the data that we need
            if (string.IsNullOrEmpty(milestoneNumber) || milestoneNumber == NextMilestone)
            {
                milestone = _milestoneProvider.GetMilestone(NextMilestone);
                issues = _issueProvider.GetAllIssuesByMilestoneThatHasTag(milestone.Number, _packageProvider.GetAllPackagesTags());
                if (!issues.Any())
                {
                    milestone = _milestoneProvider.GetLatestMilestoneWithIssues("closed");
                    issues = _issueProvider.GetAllIssuesByMilestoneThatHasTag(milestone.Number, _packageProvider.GetAllPackagesTags());
                }
            }
            else
            {
                milestone = _milestoneProvider.GetMilestone(milestoneNumber);
                issues = _issueProvider.GetAllIssuesByMilestoneThatHasTag(milestone.Number, _packageProvider.GetAllPackagesTags());
            }
            var packageCategories = _packageProvider.GetAllPackagesGroupedByCategory();
            var packageTags = _packageProvider.GetAllPackagesTags();
            
            // Lets map it to the format we need
            var release = new Release
            {
                Milestone = MapMilestone(milestone),
                IssueReporters = MapIssueReporters(issues),
                PullRequestContributors = MapPullRequestContributors(issues, packageTags),
                PackageCategories = MapCategories(packageCategories, packageTags, issues)
            };

            return release;
        }

        private List<ReleasePackage> MapCategories(IDictionary<string, GlimpsePackageGroup> packageCategories, IList<string> packageTags, IList<GithubIssue> issues)
        {
            return packageCategories.Select(x =>
            {
                var packageGroup = x.Value;
                var packageIssues = issues.Where(i => i.Labels.Any(l => packageGroup.Tags.Contains(l.Name))).Select(y => MapIssue(y, packageTags));

                return new ReleasePackage
                {
                    AcknowledgedIssues = packageIssues.Where(i => i.State == "open").ToList(),
                    CompletedIssues = packageIssues.Where(i => i.State == "closed").ToList(),
                    PackageItem = MapPackage(packageGroup.Packages),
                    Name = packageGroup.Name
                };
            }).ToList();
        }

        private List<ReleasePackageItem> MapPackage(List<GlimpsePackage> packages)
        {
            return packages.Select(x => new ReleasePackageItem
            {
                Name = x.Title,
                Status = x.Status,
                StatusDescription = x.StatusDescription
            }).ToList();
        }

        private List<Tuple<ReleaseUser, List<ReleaseIssue>>> MapPullRequestContributors(IList<GithubIssue> issues, IList<string> packageTags)
        { 
            return issues.Where(x => x.Pull_Request.Diff_Url != null)
                .GroupBy(x => x.User, x => x)
                .ToDictionary(x => x.Key, x => x.ToList())
                .Select(x => new Tuple<ReleaseUser, List<ReleaseIssue>>(MapUser(x.Key), MapIssues(x.Value, packageTags)))
                .ToList();
        }

        private ReleaseUser MapUser(GithubUser user)
        {
            return new ReleaseUser
            {
                AvatarUrl = user.Avatar_Url,
                HtmlUrl = user.Html_Url,
                Id = user.Id,
                Login = user.Login
            };
        }

        private ReleaseIssue MapIssue(GithubIssue issue, IList<string> packageTags)
        {
            return new ReleaseIssue
            {
                Category = string.Join(", ", issue.Labels.Where(x => !packageTags.Contains(x.Name)).Select(y => y.Name).ToArray()),
                Title = issue.Title,
                IssueId = issue.Id,
                IssueLinkUrl = issue.Html_Url,
                Number = issue.Number,
                User = MapUser(issue.User),
                State = issue.State
            };
        }

        private List<ReleaseIssue> MapIssues(IList<GithubIssue> issues, IList<string> packageTags)
        {
            return issues.Select(i => MapIssue(i, packageTags)).ToList();
        }

        private List<ReleaseUser> MapIssueReporters(IList<GithubIssue> issues)
        {
            return issues.Select(x => x.User)
                .DistinctBy(x => x.Id)
                .Select(MapUser)
                .ToList();
        }

        private ReleaseMilestone MapMilestone(GithubMilestone milestone)
        {
            return new ReleaseMilestone()
            {
                ClosedIssues = milestone.Closed_Issues,
                CreatedAt = milestone.Created_At,
                Description = milestone.Description,
                DueOn = milestone.Due_On,
                Number = milestone.Number,
                OpenIssues = milestone.Open_Issues,
                State = milestone.State,
                Title = milestone.Title,
                Url = milestone.Url
            };
        }
    } 
}