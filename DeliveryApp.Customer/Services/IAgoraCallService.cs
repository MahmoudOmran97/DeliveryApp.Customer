namespace DeliveryApp.Customer.Services.Call;

public interface IAgoraCallService
{
    event Action<Exception>? CallError;
    event Action? RemoteUserJoined;
    event Action? RemoteUserLeft;

    Task JoinChannelAsync(string appId, string token, string channelName, uint uid);
    void LeaveChannel();
    void MuteLocalAudio(bool mute);
    void EnableSpeakerphone(bool enable);
}