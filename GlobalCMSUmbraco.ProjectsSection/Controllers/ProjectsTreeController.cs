using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;

namespace GlobalCMSUmbraco.ProjectsSection.Controllers
{
    [Tree("projects-section", "projects", TreeTitle = "Manage Projects", TreeGroup = "manageProjectsGroup", SortOrder = 1)]
    [PluginController("GlobalCmsProjects")]
    public class ProjectsTreeController : TreeController
    {
        private readonly IProjectsSectionService projectsSectionService;

        public ProjectsTreeController(IProjectsSectionService projectsSectionService)
        {
            this.projectsSectionService = projectsSectionService;
        }

        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            // check if we're rendering the root node's children
            if (id == Constants.System.Root.ToInvariantString())
            {
                var currentUser = base.UmbracoContext.Security.CurrentUser;

                var nodes = new TreeNodeCollection();

                // only admins can add new projects
                if (currentUser.IsAdmin())
                {
                    nodes.Add(CreateTreeNode("add", id, queryStrings, "Add new project", "icon-add", false, routePath: "projects-section/projects/add"));
                }

                // anyone with access to this section can click to view the list. the list itself will check permissions
                nodes.Add(CreateTreeNode("list", id, queryStrings, "View projects list", "icon-list", false, routePath: "projects-section/projects/list"));

                // add a node for each project that this user can access
                var projectsListResponse = projectsSectionService.GetProjectsList(base.UmbracoContext.Security.CurrentUser);
                if (projectsListResponse.Success)
                {
                    var projectsList = (List<ProjectModel>) projectsListResponse.ReturnValue;
                    foreach (var project in projectsList)
                    {
                        nodes.Add(CreateTreeNode($"{project.Id}", "-1", queryStrings, project.Code, "icon-sitemap", true, routePath: $"projects-section/projects/overview/{project.Id}"));
                    }
                }

                return nodes;
            }

            var isDeveloperEdition = Common.AppSettings.IsDeveloperEdition;

            // must be second level, so rendering the options available for each project
            if (Guid.TryParse(id, out Guid projectGuid))
            {
                // add project options as tree items
                var nodes = new TreeNodeCollection();
                nodes.Add(CreateTreeNode($"settings_{id}", id, queryStrings, "Settings", "icon-settings", false, routePath: $"projects-section/projects/settings/{id}"));
                nodes.Add(CreateTreeNode($"realm_{id}", id, queryStrings, "Realm Apps", "icon-eco", false, routePath: $"projects-section/projects/realm/{id}"));

                if (!isDeveloperEdition)
                {
                    // starter kit not relevant to developer edition
                    nodes.Add(CreateTreeNode($"starterkit_{id}", id, queryStrings, "Starter Kit", "icon-split", false, routePath: $"projects-section/projects/starterkit/{id}"));
                }
                else
                {
                    nodes.Add(CreateTreeNode($"import_{id}", id, queryStrings, "Import Content", "icon-split", false, routePath: $"projects-section/projects/import/{id}"));
                }

                nodes.Add(CreateTreeNode($"permissions_{id}", id, queryStrings, "Permissions", "icon-users", false, routePath: $"projects-section/projects/permissions/{id}"));

                return nodes;
            }

            // this tree doesn't support rendering at deeper levels
            throw new NotSupportedException();
        }

        protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
        {
            return null;
        }

        protected override TreeNode CreateRootNode(FormDataCollection queryStrings)
        {
            var root = base.CreateRootNode(queryStrings);
            root.RoutePath = "projects-section/projects/dashboard";
            return root;
        }
    }
}
