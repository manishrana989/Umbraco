using DCHMediaPicker.Core.Models;
using DCHMediaPicker.Core.Services.Interfaces;
using DCHMediaPicker.Data.Repositories.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Web;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace DCHMediaPicker.Core.Services
{
    public class ExpiryService : IExpiryService
    {
        private readonly ITrackedMediaItemRepository _trackingRepository;
        private readonly IUmbracoContextFactory _umbracoContext;
        private readonly SmtpClient _smtpClient;
        private readonly ILogger _logger;
        private readonly IUserService _userService;
        public ExpiryService(ITrackedMediaItemRepository trackingRepository, IUmbracoContextFactory umbracoContext, ILogger logger, IUserService userService)
        {
            _trackingRepository = trackingRepository;
            _umbracoContext = umbracoContext;
            _logger = logger;
            _smtpClient = new SmtpClient();
            _userService = userService;
        }

        public List<ExpiryEmailData> SendExpiryReminders()
        {
            var expiryDateCutoff = DateTime.Now.AddDays(int.Parse(Helper.GetAppSetting(Constants.ExpiryCutoffDays)));
            var expiryItems = _trackingRepository.GetExpiryItems(expiryDateCutoff);
            var emailItems = new List<ExpiryEmailData>();

            foreach (var expiryItem in expiryItems)
            {
                using (var ensuredContext = _umbracoContext.EnsureUmbracoContext())
                {
                    try
                    {
                        var contentCache = ensuredContext.UmbracoContext.Content;
                        var contentItem = contentCache.GetById(expiryItem.NodeId);
                        var imageExpiryDate = expiryItem.Expiry;
                        var expiryEmailData = new ExpiryEmailData()
                        {
                            PageUrl = contentItem.Url(null, UrlMode.Absolute),
                            PageTitle = contentItem.Name,
                            ImageTitle = expiryItem.Title,
                            ImageUrl = expiryItem.Url,
                            ExpiryDate = expiryItem.Expiry?.ToString("dd/MM/yyyy HH:mm")
                        };
                        var emailContent = GetEmailBody(expiryEmailData);
                        var from = new MailAddress(Helper.GetAppSetting(Constants.EmailFromAddress), Helper.GetAppSetting(Constants.EmailFromName));
                        var message = new MailMessage
                        {
                            From = from,
                            Subject = Helper.GetAppSetting(Constants.EmailSubject),
                            Body = emailContent,
                            IsBodyHtml = true
                        };

                        var emailTo = GetToEmailAddress(contentItem.Root().Id);
                        if (!emailTo.Any())
                        {
                            _logger.Error<ExpiryService>("No email address found for: {expiryItem}", expiryItem.Id);
                            continue;
                        }

                        foreach (string emailAddress in emailTo)
                        {
                            message.To.Add(emailAddress);
                        }

                        _smtpClient.Send(message);
                        expiryItem.ReminderSent = true;
                        _trackingRepository.Update(expiryItem);
                        emailItems.Add(expiryEmailData);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error<ExpiryService>(ex, "Error sending email id: {expiryItem}", expiryItem.Id);
                    }
                }
            }

            return emailItems;
        }

        private string GetEmailBody(ExpiryEmailData emailData)
        {
            var fullPath = HttpContext.Current.Server.MapPath(Constants.EmailPath);
            var content = System.IO.File.ReadAllText(fullPath);

            content = content.Replace("{pageUrl}", emailData.PageUrl)
                        .Replace("{pageTitle}", emailData.PageTitle)
                        .Replace("{imageTitle}", emailData.ImageTitle)
                        .Replace("{imageUrl}", emailData.ImageUrl)
                        .Replace("{expiryDate}", emailData.ExpiryDate);

            return content;
        }

        private List<string> GetToEmailAddress(int rootNodeId)
        {
            var userGroups = _userService.GetAllUserGroups();
            var userGroupsToEmail = new List<IUserGroup>();
            var emailAddresses = new List<string>();

            foreach (var userGroup in userGroups)
            {
                if (userGroup.StartContentId == rootNodeId)
                {
                    userGroupsToEmail.Add(userGroup);
                }
            }

            foreach (var userGroupToEmail in userGroupsToEmail)
            {
                var userToEmail = _userService.GetAllInGroup(userGroupToEmail.Id);
                emailAddresses.AddRange(userToEmail.Select(x => x.Email));
            }

            var editorsGroup = _userService.GetUserGroupByAlias(Helper.GetAppSetting(Constants.DCHMediaPickerUmbracoEditorGroup));
            var allEditors = _userService.GetAllInGroup(editorsGroup.Id).Select(x => x.Email).ToList();

            return allEditors.Intersect(emailAddresses).ToList();
        }
    }
}