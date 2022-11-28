using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambuTestApp
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
        BambuNetworkErrorFtpLoginDenied = -25,
    }
}
