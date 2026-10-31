#include <ntddk.h>
#include <wdmsec.h>
#include "../Native.Shared/WinLayerProtocol.h"

// Диагностическое устройство; фильтрация файлов и реестра добавляется отдельно.
static const GUID WlDeviceClass =
    { 0xd16038dc, 0x276c, 0x40d2, { 0x99, 0x2e, 0x9b, 0x11, 0x74, 0x65, 0x28, 0x9c } };
static UNICODE_STRING WlSymbolicLink = RTL_CONSTANT_STRING(L"\\DosDevices\\WinLayerControl");

DRIVER_INITIALIZE DriverEntry;
DRIVER_UNLOAD WlUnload;
DRIVER_DISPATCH WlDispatch;

/// <summary>Завершает синхронный запрос устройства.</summary>
/// <param name="irp">Обрабатываемый запрос.</param>
/// <param name="status">Код результата ядра.</param>
/// <param name="bytes">Количество возвращаемых байтов.</param>
/// <returns>Переданный код результата.</returns>
static NTSTATUS WlComplete(PIRP irp, NTSTATUS status, ULONG_PTR bytes)
{
    irp->IoStatus.Status = status;
    irp->IoStatus.Information = bytes;
    IoCompleteRequest(irp, IO_NO_INCREMENT);
    return status;
}

/// <summary>Обрабатывает диагностические обращения к устройству.</summary>
/// <param name="deviceObject">Управляющее устройство.</param>
/// <param name="irp">Входящий запрос.</param>
/// <returns>Код завершения запроса.</returns>
_Use_decl_annotations_
NTSTATUS WlDispatch(PDEVICE_OBJECT deviceObject, PIRP irp)
{
    PIO_STACK_LOCATION stack = IoGetCurrentIrpStackLocation(irp);
    WL_REQUEST request;
    WL_RESPONSE* response;
    UNREFERENCED_PARAMETER(deviceObject);

    if (stack->MajorFunction == IRP_MJ_CREATE)
        return WlComplete(irp, stack->FileObject->FileName.Length == 0
            ? STATUS_SUCCESS : STATUS_OBJECT_NAME_NOT_FOUND, 0);

    if (stack->MajorFunction == IRP_MJ_CLOSE || stack->MajorFunction == IRP_MJ_CLEANUP)
        return WlComplete(irp, STATUS_SUCCESS, 0);

    if (stack->MajorFunction != IRP_MJ_DEVICE_CONTROL ||
        stack->Parameters.DeviceIoControl.IoControlCode != WL_IOCTL_QUERY)
        return WlComplete(irp, STATUS_INVALID_DEVICE_REQUEST, 0);

    if (stack->Parameters.DeviceIoControl.InputBufferLength != sizeof(WL_REQUEST) ||
        stack->Parameters.DeviceIoControl.OutputBufferLength < sizeof(WL_RESPONSE) ||
        irp->AssociatedIrp.SystemBuffer == NULL)
        return WlComplete(irp, STATUS_INVALID_BUFFER_SIZE, 0);

    // METHOD_BUFFERED использует общую память запроса и ответа: сначала сохраняем запрос.
    RtlCopyMemory(&request, irp->AssociatedIrp.SystemBuffer, sizeof(request));
    response = (WL_RESPONSE*)irp->AssociatedIrp.SystemBuffer;
    RtlZeroMemory(response, sizeof(*response));
    response->Magic = WL_MAGIC;
    response->Version = WL_PROTOCOL_VERSION;
    response->Size = sizeof(*response);

    if (request.Magic != WL_MAGIC || request.Size != sizeof(request))
        response->Status = WL_STATUS_INVALID_REQUEST;
    else if (request.Version != WL_PROTOCOL_VERSION || request.Operation != WL_QUERY_OPERATION)
        response->Status = WL_STATUS_UNSUPPORTED_PROTOCOL;
    else
    {
        response->Status = WL_STATUS_SUCCESS;
        response->DriverVersion = 1;
        response->Capabilities = WL_CAPABILITY_DIAGNOSTICS;
        response->Architecture = 64;
    }

    return WlComplete(irp, STATUS_SUCCESS, sizeof(*response));
}

/// <summary>Удаляет диагностическое устройство и его имя.</summary>
/// <param name="driverObject">Выгружаемый драйвер.</param>
_Use_decl_annotations_
VOID WlUnload(PDRIVER_OBJECT driverObject)
{
    (void)IoDeleteSymbolicLink(&WlSymbolicLink);
    if (driverObject->DeviceObject != NULL)
        IoDeleteDevice(driverObject->DeviceObject);
}

/// <summary>Создаёт защищённый диагностический канал драйвера.</summary>
/// <param name="driverObject">Объект загружаемого драйвера.</param>
/// <param name="registryPath">Путь настроек службы драйвера.</param>
/// <returns>Результат инициализации.</returns>
_Use_decl_annotations_
NTSTATUS DriverEntry(PDRIVER_OBJECT driverObject, PUNICODE_STRING registryPath)
{
    UNICODE_STRING name = RTL_CONSTANT_STRING(L"\\Device\\WinLayerControl");
    UNICODE_STRING security = RTL_CONSTANT_STRING(L"D:P(A;;GA;;;SY)(A;;GA;;;BA)");
    PDEVICE_OBJECT device = NULL;
    NTSTATUS status;
    ULONG index;
    UNREFERENCED_PARAMETER(registryPath);

    C_ASSERT(WL_IOCTL_QUERY == CTL_CODE(WL_DEVICE_TYPE, 0x800, METHOD_BUFFERED, FILE_READ_DATA));
    for (index = 0; index <= IRP_MJ_MAXIMUM_FUNCTION; ++index)
        driverObject->MajorFunction[index] = WlDispatch;
    driverObject->DriverUnload = WlUnload;

    status = IoCreateDeviceSecure(driverObject, 0, &name, WL_DEVICE_TYPE, FILE_DEVICE_SECURE_OPEN,
        FALSE, &security, &WlDeviceClass, &device);
    if (!NT_SUCCESS(status))
        return status;

    status = IoCreateSymbolicLink(&WlSymbolicLink, &name);
    if (!NT_SUCCESS(status))
    {
        IoDeleteDevice(device);
        return status;
    }

    device->Flags &= ~DO_DEVICE_INITIALIZING;
    return STATUS_SUCCESS;
}
