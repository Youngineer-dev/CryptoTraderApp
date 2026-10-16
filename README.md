# CryptoTraderApp

WPF 및 .NET 8 기반의 암호화폐 트레이딩 및 포트폴리오 관리 데스크톱 애플리케이션입니다.

## 주요 기능
- **실시간 트레이딩 대시보드**: 실시간 시세 차트(LiveCharts2 / SkiaSharp) 및 오더북, 체결 내역 확인
- **포트폴리오 모니터링**: 보유 자산 비중 차트, 평가 손익 및 자산 현황 요약
- **자동매매 봇 관리**: 그리드, 추세추종, DCA 등 전략별 트레이딩 봇 모니터링 및 제어
- **API 연동 관리**: 바이낸스, 업비트 등 거래소 API 키 관리 및 연결 테스트

## 기술 스택
- **Framework**: .NET 8.0 (WPF)
- **MVVM Pattern**: CommunityToolkit.Mvvm
- **UI Components**: WPF-UI, HandyControl
- **Charting**: LiveChartsCore.SkiaSharpView.WPF (v2.0.5)

## 시작하기
```bash
# 빌드
dotnet build

# 실행
dotnet run
```
