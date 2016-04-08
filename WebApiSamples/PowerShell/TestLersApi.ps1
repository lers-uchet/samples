$url = "http://localhost:54025/Api.asmx"

# Создаем прокси класс для работы со службой
$webservice = New-WebServiceProxy -uri $url

# Устанавливаем куки-контейнер, необходим для авторизации пользователя
$webservice.CookieContainer = New-Object System.Net.CookieContainer

# Входим в систему ЛЭРС УЧЕТ
$response1 = $webservice.Login('demo', 'demo')

# Если произошла ошибка, прекращаем работу
if($response1.IsError -eq $true)
{
     Write-Host $response1.ErrorMessage
     exit
}

# Получаем список точек учета
$response2 = $webservice.GetMeasurePointList()

# Если список пустой, выходим
if($response2.MeasurePointList.Length -le 0)
{
    Write-Host 'Список точек учета пуст'
    exit
}

# Получаем потребление по первой точке учета из списка
$endDate = [DateTime]::Today

$startDate = New-Object DateTime($endDate.Year, $endDate.Month, 1)

$response3 = $webservice.GetMeasurePointConsumption($response2.MeasurePointList[0].Id, $startDate, $endDate, 'Day')

# Если список пустой, выходим
if($response3.Data.Length -le 0)
{
    Write-Host 'По точке учета нет данных потребления'
    exit
}

# Выводим сообщение со значением T_in за первую метку времени
Write-Host $response3.Data[0].DateTime ': ' $response3.Data[0].T_in
