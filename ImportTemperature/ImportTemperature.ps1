
    ########################################################################
    # Настройки
    ########################################################################
    
    # путь к фреймворку ЛЭРС УЧЕТ
    $LersFrameworkPath = "C:\Program Files\LERS\Common\Framework\bin\Lers.System.dll" 
    
    # Адрес сервера приложения
    $ServerAddress = 'localhost'
    
    # Порт сервера приложения
    $ServerPort = 10000
   
    # Имя входа/пароль для подключения к Серверу
    $Login = 'login'
    $PassWord = 'password'
    
    # адрес ресурса
    $url = 'http://www.pogoda.ru.net/monitor.php'
    
    # регулярное выражение, для нахождения url адреса заданного города
    # <a href="monitor.php?id=35229">Хабаровск</a>
    $regExpressionCityFirst = '(?i:<a\shref="(.+)">' 
    $regExpressionCitySecond = '</a>)'
    
    # регулярное выражение для поиска среднесуточной температуры
    #  <th class=black>5</th><td class=blue1>-21.0</td><td class=green>-15.1</td>
    $regExpressionTemperatureFirst = '(?i:<th\sclass=black>'
    $regExpressionTemperatureSecond  = '</th>\s*<td\sclass=\w+>[\d\.\-\+]+</td>\s*<td\sclass=\w+>([\d\.\-\+]+)</td>)'
        
    ##################################################################
    # Подключаемые типы
    ##################################################################
    
    try
    {
        # подключаем фреймворк
        add-type -path $LersFrameworkPath
    }
    catch
    {
        write-host 'Ошибка. Не удалось загрузить файл Lers.System.dll. Проверьте правильность расположения файла. ' + $Error[0].Exception.Message
        exit
    }
    
    ########################################################################
    # Функции
    ########################################################################
    
    # Проверяет заданы ли параметры настройки скрипта
    Function CheckParameters($url, $ServerAddress, $ServerPort, $Login, $PassWord)
    {
        try
        { 
            if ($url -eq '')
            {
                throw new-object Exception('Адрес сайта мониторинга погоды не задан.')
            }
    
            if ($ServerAddress -eq '')
            {
                throw new-object Exception('Адрес сервера ЛЭРС УЧЕТ не задан.')
            }
    
            if (($ServerPort -eq $null) -or ($ServerPort -eq 0) -or ($ServerPort -eq ''))
            {
                throw new-object Exception('Порт сервера ЛЭРС УЧЕТ не задан.')
            }
   
            if ($Login -eq '')
            {
                throw new-object Exception('Имя входа в ЛЭРС УЧЕТ не задано.')
            }
            
            if ($PassWord -eq '')
            {
                throw new-object Exception('Пароль для входа в ЛЭРС УЧЕТ не задан.')
            }
        }
        catch
        {
             throw new-object Exception('Параметры скрипта не настроены. ', $Error[0].Exception)
        }
    }
    
    ##################################################################    
    
    # возвращает содержимое html страницы по адресу url ввиде строки
    Function Get-WwwString([string]$url, [string]$Encoding = "windows-1251")
    {
        try
        {
            # создаем web client
            $wc = new-object System.Net.WebClient
            
            # устанавливаем кодировку
            $wc.Encoding = [System.Text.Encoding]::GetEncoding($Encoding)
            
            # возвращаем html страницу ввиде строки
            return $wc.DownloadString($url)
        }
        catch
        {
            throw new-object Exception('Не удалось загрузить html страницу по адресу ' + $url + '.', $Error[0].Exception)
        }
    }
    
    ##################################################################    
    
    # возращает описание исключений
    Function GetFullExceptionMessage([Exception]$exc)
    {
        if($exc -eq $null)
        {
            return ''
        }
    
        $msg =  $exc.Message
    
        if ($exc.InnerException -ne $null)
        {
            $mes = GetFullExceptionMessage $exc.InnerException
            $msg  = $msg + ' ' + $mes
        }    
        
        return  $msg
    }
    
    ##########################################################################
    
    # возвращает url адрес на страницу заданного города
    # $reply - содержимое html страницы ввиде строки
    # $gorod - имя города
    # $url - адрес страницы мониторинга погоды
    Function Get-CityUrl($reply, $gorod, $url)
    {
        try
        {
            # регулярное выражение, для нахождения url адреса заданного города
            # <a href="monitor.php?id=35229">Хабаровск</a>
            [regex]$reg = $regExpressionCityFirst + $gorod + $regExpressionCitySecond
            $match = $reg.match($reply)
        
            if(!$match.Success)
            {
                throw new-object Exception(' Url страницы не найден.')   
            }
            
            # формируем ссылку на страницу
            $uri = new-object Uri ($url)
            $urlCity = $match.Groups[1].value
            $urlCity = 'http://' + $uri.Host + '/' + $urlCity   
            
            return $urlCity
        }
        catch
        {
              throw new-object Exception('Не удалось получить ссылку на страницу города - ' + $gorod + '.', $Error[0].Exception)
        }
    }
    
    ##########################################################################
    
    # возвращает хеш-таблицу среднесуточной температуры наружного воздуха за указанный период времени 
    # dateFrom - дата начало периода
    # dateTo - дата окончания периода
    # link - url города
    Function Get-CityTemperature($dateFrom, $dateTo, $link)
    {
        try
        {
            $today = [DateTime]::Today
            if($dateFrom -gt $today)
            {
                throw new-object Exception('Дата начала периода превышает текущую.')
            }
            
            if($dateFrom -gt $dateTo)
            {
               throw new-object Exception('Дата начала периода больше даты окончания.')
            }
            
            if($dateTo -gt $today)
            {
                $dateTo = $today
            }
        
            # хеш таблица для хранения среднесуточных значениий температур по дате
            $TempTable = @{}
        
            # месяц, за который загружается html-страница с темпераиурами
            $month = 0                
            
            # html страница
            [void][string]$html
            
            while ($dateFrom -le $dateTo)
            {
                if ($dateFrom.Month -ne $month)
                {
                    $month = $dateFrom.Month
                    
                    # ссылка на страницу за указанный месяц и год
                    $ref = $link + '&month=' + $dateFrom.Month + '&year=' + $dateFrom.Year
                    
                    # загружаем html страницу заданного месяца и года
                    $html = Get-WwwString $ref     
                }
            
                # регулярное выражение для поиска среднесуточной температуры
                #  <th class=black>5</th><td class=blue1>-21.0</td><td class=green>-15.1</td>
                [regex]$reg = $regExpressionTemperatureFirst + $dateFrom.Day  + $regExpressionTemperatureSecond
                $match = $reg.match($html)
    
                if (!$match.Success)
                {
                    # температура не найдена
                    throw new-object Exception('Температура не найдена.')   
                }
                
                # считываем температуру
                $T = $match.Groups[1].value
                
                # сохраняем температуру
                $TempTable[$dateFrom.ToString()] = $T
            
                $dateFrom = $dateFrom.AddDays(1)
            }
            
            return $TempTable
        }
        catch
        {
              throw new-object Exception('Ошибка получения среднесуточной температуры за указанный период. ', $Error[0].Exception)
        }
    }   
    
    ##########################################################################
    
    # возвращает входные параметры
    Function Arguments($inArgs)
    {
        try
        {
            # проверяем количество параметров
            
            if($inArgs.Length -eq 0)
            {
                throw new-object Exception('Отсутствуют параметры командной строки.')
            }
            
            # возвращает введенное имя города
            $gorod = $inArgs[0]
            $gorod
            
            [DateTime]$dateTo = [System.DateTime]::Today.AddDays(-1)
            [DateTime]$dateFrom = $dateTo


            if($inArgs.Length -eq 2)
            {
                $DateTimeStr = $inArgs[1]
                $DateTimeStr = $DateTimeStr.Split('-', [System.StringSplitOptions]::RemoveEmptyEntries)    
                
                if($DateTimeStr.Length -ne 2)
                {
                    throw new-object Exception('Период времени задан неверно.')
                }
                
                # дата начало периода
                if (![DateTime]::TryParse($DateTimeStr[0], [ref]$dateFrom))
                {
                    throw new-object Exception('Дата начала периода задана в неверном формате. Формат даты: dd.mm.yyyy')
                }
                
                
                # дата конца периода
                if (![DateTime]::TryParse($DateTimeStr[1], [ref]$dateTo))
                {
                    throw new-object Exception('Дата окончания периода задана в неверном формате. Формат даты: dd.mm.yyyy')
                }
            }
            
    
            # возвращаем дату начало/конца
            $dateFrom
            $dateTo
        }
        catch
        {
              throw new-object Exception('Ошибка разбора параметров коммандной строки. ', $Error[0].Exception)
        }
    }
    
    ##########################################################################
      
    # Функция подключается к серверу автоматизации и возвращает объект сервера
    Function ConnectToServer()
    {
        try
        {
            Write-Host 'подключаемся к серверу по адресу ' $ServerAddress ':' $ServerPort
            
            $securePassword = [Lers.Networking.SecureStringHelper]::ConvertToSecureString($PassWord)

			$authenticationInfo = New-Object Lers.Networking.BasicAuthenticationInfo($Login, $securePassword)
					
			# подключаемся к серверу
            $server = new-object Lers.LersServer
			
			$server.Connect($ServerAddress, $ServerPort, $authenticationInfo)
            return $server
        }
        catch
        {
            throw new-object Exception('Не удалось подключится к серверу ЛЭРС УЧЕТ. ', $Error[0].Exception)
        }
    }  
    
    ##########################################################################
    
    # функция сохраняет данные о температуре на сервере ЛЭРС УЧЕТ
    # $server - сервер автоматизации
    # $tempTable - таблица среднесуточных температур
    Function SaveTemperatureToServer ($server, $tempTable)
    {
        Write-Host 'Cохраняем данные на сервере ЛЭРС УЧЕТ'
        try
        {
            $str =''
            $keys = $tempTable.Keys
            
            $temperature = New-Object Lers.Data.OutdoorTemperatureRecord[] $keys.Count
            $i = 0

            foreach($key in $keys)
            {
                $dt = [DateTime]::Parse($key)

                $value = [string]$tempTable[$key]
                
                $separator = [System.Globalization.CultureInfo]::CurrentCulture.NumberFormat.NumberDecimalSeparator;
                
                $value = [Convert]::ToSingle($value.Replace(".",$separator))

				$record = New-Object Lers.Data.OutdoorTemperatureRecord($dt)
				$record.Value = $value
				
                $temperature[$i] = $record    

                $i++
            }

            # сохранение данных на сервере
            $server.OutdoorTemperature.Set($temperature)
        }
        catch
        {
            throw new-object Exception('Не удалось сохранить данные среднесуточной температуры на сервере ЛЭРС учет. ', $Error[0].Exception)
        }
    }
    
    ##########################################################################
    # Точка входа
    ##########################################################################
    
    try
    {
        Write-Host 'Запуск импорта данных среднесуточной температуры с сайта мониторинга погоды.' 

        # проверяем параметры настройки скрипта
        CheckParameters $url $ServerAddress $ServerPort $Login $PassWord
        
        #param[0] = город, param[1] = дата_начала, param[2] = дата_окончания
        [object[]]$param = Arguments $Args
        
        Write-host 'Загружаем ресурс ' $url 
        
        # загружаем html страницу        
        $reply = Get-WwwString $url
        
        # получаем ссылку на страницу, указанного города
        $link = Get-CityUrl $reply $param[0] $url
        
        # загружаем таблицу с температурами наружного воздуха
        $tempTable = Get-CityTemperature $param[1] $param[2] $link 
        
        write-host 'Получены среднесуточные температуры.'
        
        # подключаемся к серверу ЛЭРС
        $server = ConnectToServer
        
        # сохраняем температуру на сервере
        SaveTemperatureToServer $server $tempTable
        
        Write-Host 'Импорт температур успешно завершен.'
    }
    catch
    {
        write-host 'Ошибка импорта температур. ' (GetFullExceptionMessage $Error[0].Exception)
        exit
    }
    
    ########################################################################