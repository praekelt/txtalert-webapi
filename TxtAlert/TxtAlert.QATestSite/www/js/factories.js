angular.module('txtAlertQA')
.factory('apiFactory', function () {
    var apiDetails = {
        url: 'http://localhost:62489/api/'
    };

    var apiControllers = [
        {
            description: 'AppAd',
            controller: 'appad',
            actions: [
                {
                    description: 'Get All Data',
                    url: ''
                },
                {
                    description: 'Patient List',
                    url: 'patientlist'
                },
                {
                    description: 'Coming Visits',
                    url: 'comingvisits'
                },
                {
                    description: 'Missed Visits',
                    url: 'missedvisits'
                },
                {
                    description: 'Done Visits',
                    url: 'donevisits'
                },
                {
                    description: 'Deleted Visits',
                    url: 'deletedvisits'
                },
                {
                    description: 'Rescheduled Visits',
                    url: 'rescheduledvisits'
                }
            ]
        }
    ];

    return {
        apiDetails: apiDetails,
        apiControllers: apiControllers
    };
});