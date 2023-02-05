using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambuTestApp
{
    public class BambuEnums
    {
        public enum NetworkStatus
        {
            BambuNetworkSuccess = 0,
            BambuNetworkErrorInvalidHandle = -1,
            BambuNetworkErrorConnectFailed = -2,
            BambuNetworkErrorDisconnectFailed = -3,
            BambuNetworkErrorSendMessageFailed = -4,
            BambuNetworkErrorBindFailed = -5,
            BambuNetworkErrorUnbindFailed = -6,
            BambuNetworkErrorPrintFailed = -7,
            BambuNetworkErrorLocalPrintFailed = -8,
            BambuNetworkErrorRequestSettingFailed = -9,
            BambuNetworkErrorPutSettingFailed = -10,
            BambuNetworkErrorGetSettingListFailed = -11,
            BambuNetworkErrorDeleteSettingFailed = -12,
            BambuNetworkErrorGetUserPrintInfoFailed = -13,
            BambuNetworkErrorGetPrinterFirmwareFailed = -14,
            BambuNetworkErrorQueryBindInfoFailed = -15,
            BambuNetworkErrorModifyPrinterNameFailed = -16,
            BambuNetworkErrorFileDoesNotExist = -17,
            BambuNetworkErrorFileOverSize = -18,
            BambuNetworkErrorCheckMd5Failed = -19,
            BambuNetworkErrorTimeout = -20,
            BambuNetworkErrorCanceled = -21,
            BambuNetworkErrorInvalidParams = -22,
            BambuNetworkErrorInvalidResult = -23,
            BambuNetworkErrorFtpUploadFailed = -24,
            BambuNetworkErrorFtpLoginDenied = -25
        }


        // enums
        public enum SendingPrintJobStage
        {
            PrintingStageCreate = 0,
            PrintingStageUpload = 1,
            PrintingStageWaiting = 2,
            PrintingStageSending = 3,
            PrintingStageRecord = 4,
            PrintingStageFinished = 5
        }

        public enum PublishingStage
        {
            PublishingCreate = 0,
            PublishingUpload = 1,
            PublishingWaiting = 2,
            PublishingJumpUrl = 3
        }

        public enum BindJobStage
        {
            LoginStageConnect = 0,
            LoginStageLogin = 1,
            LoginStageWaitForLogin = 2,
            LoginStageGetIdentify = 3,
            LoginStageWaitAuth = 4,
            LoginStageFinished = 5
        }

        public enum ConnectStatus
        {
            ConnectStatusOk = 0,
            ConnectStatusFailed = 1,
            ConnectStatusLost = 2
        }
    }
}