# Логотип EdgeGlow

Два расположенных друг над другом экрана объединены цветным свечением на стыке. Тёмная основа сохраняет контраст, тёплый левый край переходит в холодный правый.

- `assets/logo.png` — исходное изображение для README, GitHub и оформления проекта.
- `assets/edgeglow.ico` — Windows-иконка с размерами 16/24/32/48/64/128/256 px; встроена в EXE, окно настроек и трей.
- `tools/export-icon.ps1` — воспроизводимое создание ICO из PNG на Windows. Это изменение размера и формата, без перерисовки логотипа.

Происхождение: встроенный инструмент ImageGen, не внешний CLI. Логотип сгенерирован для проекта, не заимствован из Ambient Monitor. Файл является растровым изображением; SVG-оригинал не заявляется.

## Промпт генерации

```text
Use case: logo-brand. Asset type: one finished app icon for EdgeGlow, a Windows app that projects soft colored light from the seam of two adjacent monitors. Create a refined, distinctive square 1024x1024 app icon with NO text. One centered symbol: two minimal wide rounded display frames stacked vertically, with a narrow shared horizontal seam emitting a soft aurora glow, warm coral-magenta at the left smoothly flowing into electric cyan-blue at the right. The seam glow rises gently into the upper display; the lower display is subtle. Strong simple silhouette, confidently thick luminous edges, dark midnight navy rounded-square tile that fills the image, generous but not excessive margin, front-on flat geometry, premium restrained luminous gradients. Crisp, legible at 32px. Do not include monitor stands, perspective, letters, words, mockup sheets, extra decorations, tiny detail, watermarks, or a surrounding white margin. The result must be a single production-ready polished icon, not a presentation board.
```
