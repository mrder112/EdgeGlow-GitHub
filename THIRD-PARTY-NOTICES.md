# Сторонние компоненты

EdgeGlow использует следующие библиотеки; полные условия включены в папку `licenses`.

| Компонент | Версия | Лицензия |
|---|---|---|
| Vortice.Direct3D11, Vortice.DXGI, Vortice.DirectX, Vortice.D3DCompiler | 3.8.3 | MIT |
| Vortice.Mathematics | 2.1.0 | MIT |
| SharpGen.Runtime, SharpGen.Runtime.COM | 2.4.2-beta | MIT |
| Microsoft .NET Runtime / Windows Desktop Runtime | 10.0.9 | MIT и уведомления включённых компонентов |

Библиотеки Windows `user32`, `gdi32`, `dwmapi`, `dxgi`, `d3d11`, `d3dcompiler_47`, `wtsapi32` поставляются операционной системой, не копируются из чужих приложений.

Исходный проект [Ambient Monitor](https://github.com/CasketPizza/AmbientMonitor) просмотрен для сравнения подхода. Его лицензия GPL-3.0; его код и ресурсы не используются и не распространяются в EdgeGlow.
