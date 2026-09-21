# Как выложить EdgeGlow

Пакет репозитория не содержит локальных настроек, истории переписки, диагностических отчётов, исполняемых файлов или среды разработки. Portable-файлы публикуются отдельно в Releases.

## Через сайт GitHub

1. Создайте пустой репозиторий, например `EdgeGlow`. Не добавляйте автоматически README или лицензию — они уже есть в пакете.
2. Распакуйте `EdgeGlow-GitHub.zip`.
3. Загрузите **содержимое** папки `EdgeGlow-GitHub` в корень репозитория через **Add file → Upload files**. Сам ZIP не заменяет исходники в репозитории.
4. Проверьте наличие папки `.github/workflows`, файлов `.gitignore` и `.gitattributes`. Проводник или веб-загрузка могут скрывать dotfiles; при сомнении используйте GitHub Desktop или git.
5. После коммита на вкладке **Actions** проверьте сборку Windows. Workflow не публикует Release автоматически.
6. В **Releases → Draft a new release** создайте тег `v0.1.0` и приложите `EdgeGlow-portable-win-x64.zip` вместе с `SHA256SUMS.txt`. Текст можно взять из `RELEASE_NOTES.md`.
7. При желании используйте `assets/logo.png` как изображение проекта. Версии ICO и исходный промпт находятся в `assets/` и `docs/BRANDING.md`.

## Изображения в Release

В папке `assets/release/` лежат обложка `edgeglow-cover.png` и наглядная визуализация `edgeglow-demo.png`. Они уже встроены в README. При создании Release перетащите PNG в поле описания: GitHub загрузит их и вставит рабочие ссылки. В `RELEASE_NOTES.md` отмечены места для картинок и подготовлены подписи. Сохраните подписи: обе картинки являются иллюстрациями, не измерением или скриншотом реального эффекта. Прикрепление PNG только в список файлов Release не добавляет их в текст описания.

## Через git

В распакованной папке выполните:

```powershell
git init -b main
git add .
git commit -m "Initial EdgeGlow release"
# В следующей строке замените YOUR-NAME своим именем на GitHub:
git remote add origin https://github.com/YOUR-NAME/EdgeGlow.git
git push -u origin main
```

Если git попросит имя и почту автора, настройте собственные значения локально. В пакет не включены чужие учётные данные и заранее созданные коммиты.

## Лицензия и авторство

Подготовлена лицензия MIT с нейтральной строкой `EdgeGlow contributors`. Перед публикацией её можно заменить своим именем или названием организации. Полные лицензии сторонних библиотек следует сохранить. Логотип создан с помощью генерации изображений; сведения о происхождении включены в пакет.

Рекомендуемое описание репозитория:

> Ambient edge glow across multiple monitors for Windows 11. Local capture, transparent overlays, configurable performance.

Темы: `windows`, `ambient-lighting`, `multi-monitor`, `screen-capture`, `direct3d11`, `csharp`, `winforms`.
