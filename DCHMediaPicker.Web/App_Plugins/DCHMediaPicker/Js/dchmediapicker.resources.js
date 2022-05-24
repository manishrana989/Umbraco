angular.module("umbraco.resources").factory("DCHMediaPickerResources", function ($http, iconHelper) {
    return {
        basicSearch: function (q, nodeId, page = 1) {
            return $http.get(`backoffice/DCHMediaPicker/Api/BasicSearch?q=${q}&page=${page}&nodeId=${nodeId}`)
                .then(function (response) {
                    return response;
                });
        },
        advancedSearch: function (data) {
            return $http.post('backoffice/DCHMediaPicker/Api/AdvancedSearch', data)
                .then(function (response) {
                    return response;
                });
        },
        dashboard: function (page, projectId, showExpired) {
            return $http.get(`backoffice/DCHMediaPicker/Api/Dashboard?page=${page}&projectId=${projectId}&showExpired=${showExpired}`)
                .then(function (response) {
                    return response;
                });
        },
        reminders: function () {
            return $http.get("backoffice/DCHMediaPicker/Api/SendEmails")
                .then(function (response) {
                    return response;
                });
        },
        collections: function (nodeId) {
            return $http.get(`backoffice/DCHMediaPicker/Api/Collections?nodeId=${nodeId}`)
                .then(function (response) {
                    return response;
                });
        },
        projects: function () {
            return $http.get('backoffice/DCHMediaPicker/Api/Projects')
                .then(function (response) {
                    return response;
                });
        },
        resolveItem: function (data, nodeId) {
            return $http.post(`backoffice/DCHMediaPicker/Api/ResolveItem?nodeId=${nodeId}`, data)
                .then(function (response) {
                    return response;
                });
        }
    }
});