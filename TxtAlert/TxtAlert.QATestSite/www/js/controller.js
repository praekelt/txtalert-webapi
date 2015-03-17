angular.module('txtAlertQA', ['ngTable'])
.controller('appController', function ($scope, $http, ngTableParams, apiFactory) {
    $scope.apiControllers = apiFactory.apiControllers;
    $scope.apiDetails = apiFactory.apiDetails;
    $scope.selected = {
        controller: 'appad',
        action: ''
    };

    $scope.visits = null;

    $scope.getData = function () {
        var url = $scope.apiDetails.url + $scope.selected.controller + '/' + $scope.selected.action;

        $http.get(url, {}, {})
        .success(function (data, status, headers, blah) {
            $scope.visits = data;
        }).error(function (data, status, headers, blah) {
            debugger;
        });
    };

    $scope.txtAlertTable = new ngTableParams({
        page: 1,            // show first page
        count: 10,          // count per page
        filter: {
            Cellphone_number: '',
            Data_Extraction: '',
            Facility_name: '',
            File_No: '',
            Next_tcb: '',
            Ptd_No: '',
            Received_sms: '',
            Return_date: '',
            Status: '',
            Visit: '',
            Visit_date: ''
        },
        sorting: {
            FamilyReferenceNumber: 'asc'
        }
    }, {
        total: 0, // length of data
        getData: function ($defer, params) {
            $timeout(function () {
                var filteredData = params.filter() ?
                    $filter('filter')($scope.visits, params.filter()) :
                    $scope.lookup.households;

                var orderedData = params.sorting() ?
                        $filter('orderBy')(filteredData, params.orderBy()) :
                        filteredData;

                params.total(orderedData.length);
                var currentPage = orderedData.slice((params.page() - 1) * params.count(), params.page() * params.count()); 4
                $defer.resolve(currentPage);
            }, 0);
        }
    });
});