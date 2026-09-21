# Архитектура

`Engine` управляет одним источником и набором принимающих наложений. Данные экранов берутся из Windows в физических координатах PerMonitorV2.

1. `Capture` создаёт D3D11-устройство на адаптере источника и одну DXGI Desktop Duplication-сессию.
2. Полученная текстура копируется GPU→GPU для shader-resource view. Compute shader обрабатывает четыре полосы с учётом поворота и обрезки; результат имеет размер N×4, где N = 32/64/128/256.
3. На CPU читается 2–16 КиБ цветовых данных за кадр. Полноразмерные снимки экрана на CPU не передаются.
4. `Geometry` определяет общий стык и преобразования; `Overlay` размывает маленький сигнал и выполняет временное сглаживание с учётом dt.
5. `Raster` векторизованно заполняет постоянную premultiplied BGRA DIB. Цвет и затухание сохраняют float-точность до финального 8-битного квантования с неподвижным дизерингом.
6. `UpdateLayeredWindow` передаёт поверхность композитору Windows. Это гибридный GPU/CPU подход, не полностью GPU-рендеринг.

Кадры не ставятся в неограниченную очередь. Старое наложение удаляется до смены источника. `WDA_EXCLUDEFROMCAPTURE` проверяется, но не является единственной защитой от обратной связи: окна не создаются на источнике.

## Визуальное смешивание и ввод

Используется обычное premultiplied alpha-over. Непрозрачность зависит от яркости источника и расстояния, поэтому чёрное изображение не создаёт тёмную полосу. Additive/screen-смешивание не заявляется.

`WS_EX_LAYERED` обеспечивает per-pixel alpha, `WS_EX_TRANSPARENT` — пропуск мыши для layered-окна, `WS_EX_NOACTIVATE` — неактивацию, `WS_EX_TOOLWINDOW` — исключение из панели задач/Alt+Tab. Режим рабочего стола консервативно скрывает весь ореол получателя при пересечении его области обычным окном.

## Источник и события

`ClickTracker` наблюдает только координату нажатия кнопки мыши, не подавляет ввод и ничего не записывает. `InteractionGuard` наблюдает системные события начала/конца перемещения окна. При включённом захвате во время перемещения они не скрывают эффект.

Есть обработка блокировки, сна, изменения экранов и повторное создание захвата после ошибки. Это не заменяет натурные проверки всех драйверов и конфигураций.

## API и библиотеки

- [Microsoft Desktop Duplication](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/desktop-dup-api)
- [UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow)
- [Layered windows и ввод](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features)
- [SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)
- [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)
