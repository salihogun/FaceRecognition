using System.Windows;
using FaceRecognition.Views;
using Prism.Ioc;

namespace FaceRecognition
{
    public partial class App
    {
        /// <inheritdoc />
        protected override void RegisterTypes(IContainerRegistry o)
        {
            o.RegisterInstance(
                new Config
                {
                    TimerResponseValue = 50,
                    ImageFileExtension = ".bmp",
                    ActiveCameraIndex = 0,
                    FacePhotoPath = "Source\\Faces\\",
                    FaceListTextFile = "Source\\FaceList.txt",
                    HaarCascadePath = "Resources\\haarcascade_frontalface_default.xml",
                });
        }

        /// <inheritdoc />
        protected override Window CreateShell()
        {
            return Container.Resolve<ShellView>();
        }
    }
}