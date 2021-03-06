## Настройки VPN аккаунтов

![Home security mail fetch hub](https://claudiacoord.github.io/SecurityHomeMailHub/assets/images/hsmh10-Settings-VPN-accounts.png)

```
 Настройте подключение к серверам туннелей WireGuard VPN,
 которые используют современную криптографию.
 Рекомендуется импортировать конфигурационные файлы WireGuard (.conf) или
 скопировать их содержимое с помощью кнопки «Вставить».
```

Узнайте больше о туннелях `WireGuard`: https://www.wireguard.com/

#### Общедоступная конечная точка (Конечная точка):

IP-адрес и порт сервера доступа `VPN`.

#### Закрытый ключ (Закрытый ключ):

Закрытый ключ WireGuard для одного хоста, предоставляемый провайдером `VPN`, уникален.

#### Общий ключ:

Предварительно общий ключ сервера `WireGuard`, предоставленный провайдером `VPN`, уникален.

#### Открытый ключ: (Открытый ключ):

Открытый ключ `WireGuard` для одного хоста, предоставляемый провайдером `VPN`, уникален.

#### Айпи адрес:

Адреса, к которым будет привязан клиент, либо `IPv4`, либо `IPv6`. Предоставляется провайдером `VPN`.

#### Список разрешенных сетей для использования VPN (AllowedIPs):

Диапазон IP-адресов, трафик которых направляется в туннель `VPN`.

#### DNS

Сервер доменных имен, используемый для преобразования имен хостов в IP-адреса для клиентов `VPN`.
Рекомендуется оставить значение, предоставленное провайдером `VPN`, это предотвратит утечку запросов `DNS` за пределы `VPN` и раскрытие трафика. Утечки можно проверить с помощью [http://dnsleak.com](http://dnsleak.com).

#### Период проверки VPN-соединения (Keepalive):

Отправляйте периодические сообщения проверки активности, чтобы обеспечить рабочее соединение.

#### Импорт Экспорт:

Можно импортировать или экспортировать выбранную учетную запись в формате `Wireguard` (`*.conf`).