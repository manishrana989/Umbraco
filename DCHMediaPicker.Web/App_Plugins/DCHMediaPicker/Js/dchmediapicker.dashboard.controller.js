(function () {
    "use strict";

    function DCHMediaPickerDashboard($scope, DCHMediaPickerResources) {
        var vm = this;
        vm.sendReminderEmails = sendReminderEmails;
        vm.changeTab = changeTab;
        vm.page = page;
        vm.getProjects = getProjects;
        vm.tab = 'trackedItems';
        vm.selectedProject = null;
        vm.showExpired = false;
        vm.getItems = getItems;
        vm.displayDate = displayDate;
        vm.getProjects();

        function sendReminderEmails() {
            DCHMediaPickerResources.reminders()
                .then(function (response) {
                    console.log(response);
                });
        }

        function changeTab(tab) {
            vm.tab = tab;
        }

        function page(page = 1) {
            DCHMediaPickerResources.dashboard(page, vm.selectedProject, vm.showExpired)
                .then(function (response) {
                    vm.items = response.data.Items;
                    vm.paging = Array(response.data.TotalPages).fill().map((x, i) => i + 1);
                    vm.currentPage = response.data.CurrentPage;
                });
        }

        function getProjects() {
            DCHMediaPickerResources.projects()
                .then(function (response) {
                    vm.projects = response.data;
                });
        }

        function getItems() {
            page();
        }

        function displayDate(date) {
            var timestamp = Date.parse(date);
            var naText = 'n/a';

            if (!isNaN(timestamp)) {
                var expiryDate = new Date(timestamp);
                if (expiryDate.getFullYear() === 9999) {
                    return naText;
                }
                else {
                    return expiryDate.toDateString();
                }
            }

            return naText;
        }
    }

    angular.module("umbraco").controller("Dare.DCHMediaPicker.Dashboard", DCHMediaPickerDashboard);
})();