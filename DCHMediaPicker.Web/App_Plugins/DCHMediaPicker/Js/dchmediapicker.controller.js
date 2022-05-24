(function () {
    "use strict";

    function DCHMediaPicker($scope, editorService, mediaS3ConfigApiService) {
        if (typeof($scope.model.value) === 'undefined' || $scope.model.value.constructor !== Array) {
            $scope.model.value = [];
        }

        // Populate the bucketName and mediaPrefix
        var vm = this;
        mediaS3ConfigApiService.get().then(function (response) {
            vm.bucketName = response.data.S3BucketName;
            vm.mediaPrefix = response.data.S3MediaPrefix;
            vm.mediaDomain = response.data.MediaDomain;

            $scope.model.value.forEach(function (image) {
                enrichImage(image);
            });
        });

        $scope.add = function () {
            openMediaPicker();
        };

        $scope.options = function (image) {
            openImageOptions(image);
        }

        $scope.remove = function (i) {
            $scope.model.value.splice(i, 1);
            updateCount();
        };

        $scope.validate = function () {
            if ($scope.model.value.length < $scope.model.config.minItems) {
                return {
                    isValid: false,
                    errorMsg: `You need at least ${$scope.model.config.minItems} items`,
                    errorKey: "required"
                };
            }
            else if ($scope.model.config.maxItems !== 0 && $scope.model.value.length > $scope.model.config.maxItems) {
                return {
                    isValid: false,
                    errorMsg: `You have too many items - ${$scope.model.config.maxItems} maximum`,
                    errorKey: "required"
                };
            }

            return {
                isValid: true,
                errorMsg: "required",
                errorKey: "required"
            };
        }

        function openMediaPicker() {
            var settingsEditor = {
                title: "DCH Media Picker",
                view: "/App_Plugins/DCHMediaPicker/Views/dialog.html",
                size: "medium",
                submit: function (model) {
                    enrichImage(model)
                    $scope.model.value.push(model);
                    editorService.close();
                    updateCount();
                },
                close: function () {
                    editorService.close();
                }
            };

            editorService.open(settingsEditor);
        }

        function updateCount() {
            $scope.propertyForm.itemCount.$setViewValue($scope.model.value.length);
        }

        function openImageOptions(image) {
            editorService.open({
                view: Umbraco.Sys.ServerVariables.application.applicationPath + 'App_Plugins/GlobalCMS/AwsSihOptions/views/globalcms.awssihoptions.editor.html',
                size: 'large',
                value: image.imageOptions,
                submitCallback: function (modelValue) {
                    image.imageOptions = modelValue;
                    editorService.close();
                },
                closeCallback: function () {
                    editorService.close();
                }
            });
        }

        function enrichImage(image) {

            image.imageOptions = image.imageOptions || {};
            image.imageOptions.default = image.imageOptions.default || {};
            image.imageOptions.default.bucket = image.imageOptions.default.bucket || vm.bucketName;
            image.imageOptions.default.key = image.imageOptions.default.key || getS3Key(image);
            image.imageOptions.mediaDomain = vm.mediaDomain;
        }

        function getS3Key(image) {
            var url = new URL(image.Url);
            return decodeURIComponent(url.pathname.replace(/^\/+/, ''));
        }
    }

    angular.module("umbraco").controller("Dare.DCHMediaPicker", DCHMediaPicker);
})();