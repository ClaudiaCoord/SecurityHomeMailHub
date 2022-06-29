## Настройки почтовых клиентов :: Опции

![Home security mail fetch hub](https://claudiacoord.github.io/SecurityHomeMailHub/assets/images/hsmh4-Settings-mail-clients.png)

#### Максимальное время ожидания для почтовых клиентов:

Время ожидания для состояния «`ПОДКЛЮЧЕНИЕ`» в миллисекундах, значение по умолчанию — `240000` (`240 секунд`).
Время задержки в состоянии «`ОТПРАВИТЬ`» составляет половину указанного значения.

#### Период проверки почты:

Расписание проверки почты на внешних серверах указывается в минутах, по умолчанию `1440` минут (`24 часа`).

#### Всегда очищать сообщения:

Правило распространяется на протокол `IMAP` для получения почты с внешних серверов.
Принудительно удаляет полученные сообщения с внешнего сервера.

#### Всегда добавлять поддельный IP-адрес в заголовки сообщений:

Правило действует для протокола `SMTP` при отправке почты на внешние серверы.
Добавляет в заголовки сообщения фиктивный IP-адрес отправителя, равный адресу последнего прокси-сервера, через который было осуществлено подключение.

#### Получать внешнюю почту только при отправке сообщений:

Правило отключает расписание получения почты с внешних серверов.
Фактическое получение почты произойдет только тогда, когда появится письмо, требующее отправки через внешних почтовых провайдеров.

#### Ассоциация файлов:

Позволяет связать типы файлов (`*.eml`, `*.msg`) с этим приложением.
Это позволит открывать файлы с вышеуказанными расширениями в этом приложении.