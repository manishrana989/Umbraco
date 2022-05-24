(function () {
    "use strict";

    function DCHMediaPickerDialog($scope, $routeParams, DCHMediaPickerResources, overlayService) {
        var vm = this;
        vm.submit = submit;
        vm.close = close;
        vm.search = search;
        vm.clear = clear;
        vm.page = page;
        vm.keyup = keyup;
        vm.changeTab = changeTab;
        vm.loading = false;
        vm.searched = false;
        vm.tab = 'basic';
        $scope.model.selectedItem = "";
        getCollections();

        function getCollections() {
            DCHMediaPickerResources.collections($routeParams.id)
                .then(function (response) {
                    vm.collections = response.data;
                });
        }

        function submit() {
            if ($scope.model.submit) {
                DCHMediaPickerResources.resolveItem($scope.model.selectedItem, parseInt($routeParams.id))
                    .then(function (response) {
                        if (!response.data.Resolved) {
                            overlayService.open({
                                content: `There was an error when trying to resolve the item, ID: ${response.data.Id}`,
                                closeButtonLabel: "Close",
                                close: function () {
                                    overlayService.close();
                                }
                            });
                        }
                        else {
                            $scope.model.submit(response.data);
                        }
                    });
            }
        }

        function close() {
            if ($scope.model.close) {
                $scope.model.close();
            }
        }

        function search(page) {
            vm.loading = true;
            vm.searched = true;

            if (vm.tab === 'basic') {
                basicSearch(page);
            }
            else if (vm.tab === 'advanced') {
                advancedSearch(page);
            }
            else if (vm.tab === 'collection') {
                collectionSearch(page);
            }
        }

        function basicSearch(page) {
            DCHMediaPickerResources.basicSearch($scope.model.searchTerms, $routeParams.id, page)
                .then(function (response) {
                    vm.searchResults = response.data;
                    vm.paging = Array(response.data.TotalPages).fill().map((x, i) => i + 1);
                    vm.currentPage = response.data.CurrentPage;
                    vm.loading = false;
                });
        }

        function advancedSearch(page = 1) {
            var data = {
                searchTerms: $scope.model.searchTerms,
                projectName: $scope.model.projectName,
                assetType: $scope.model.assetType,
                tags: $scope.model.tags,
                fileType: $scope.model.fileType,
                keywords: $scope.model.keywords,
                page: page,
                nodeId: $routeParams.id
            };

            DCHMediaPickerResources.advancedSearch(data)
                .then(function (response) {
                    vm.searchResults = response.data;
                    vm.paging = Array(response.data.TotalPages).fill().map((x, i) => i + 1);
                    vm.currentPage = response.data.CurrentPage;
                    vm.loading = false;
                });
        }

        function collectionSearch(page = 1) {
            var data = JSON.parse($scope.model.collection);

            if (data.collection === 'Modified') {
                data.modified = {
                    start: getTodayString(),
                    end: getPastString(data.days)
                }
            }

            data.page = page;
            data.nodeId = $routeParams.id;

            DCHMediaPickerResources.advancedSearch(data)
                .then(function (response) {
                    vm.searchResults = response.data;
                    vm.paging = Array(response.data.TotalPages).fill().map((x, i) => i + 1);
                    vm.currentPage = response.data.CurrentPage;
                    vm.loading = false;
                });
        }

        function page(page) {
            search(page);
        }

        function clear() {
            $scope.model.searchTerms = "";
            $scope.model.projectName = "";
            $scope.model.assetType = "";
            $scope.model.tags = "";
            $scope.model.fileType = "";
            $scope.model.keywords = "";
        }

        function keyup(event) {
            if (event.keyCode === 13) {
                search();
            }
        }

        function changeTab(tab) {
            vm.tab = tab;
        }

        function getTodayString() {
            return new Date().toISOString();
        }

        function getPastString(daysToAdd) {
            var theDate = new Date();
            theDate.setDate(theDate.getDate() + daysToAdd); 
            return theDate.toISOString();
        }
    }

    angular.module("umbraco").controller("Dare.DCHMediaPicker.Dialog", DCHMediaPickerDialog);
})();