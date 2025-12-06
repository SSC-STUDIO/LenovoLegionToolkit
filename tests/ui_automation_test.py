"""
Lenovo Legion Toolkit UI自动化测试脚本
使用 pywinauto 或 pyautogui 进行UI自动化测试

安装依赖:
    pip install pywinauto pyautogui

注意: 此脚本需要应用程序正在运行
"""

import time
import sys
import os
from pathlib import Path

try:
    from pywinauto import Application
    PYWINAVAILABLE = True
except ImportError:
    PYWINAVAILABLE = False
    print("警告: pywinauto 未安装，UI自动化测试将跳过")
    print("安装命令: pip install pywinauto")

try:
    import pyautogui
    PYAUTOGUIAVAILABLE = True
except ImportError:
    PYAUTOGUIAVAILABLE = False
    print("警告: pyautogui 未安装，部分UI测试将跳过")
    print("安装命令: pip install pyautogui")


class UITestRunner:
    def __init__(self, exe_path=None):
        self.exe_path = exe_path or r"build\Lenovo Legion Toolkit.exe"
        self.app = None
        self.results = {
            "passed": [],
            "failed": [],
            "skipped": []
        }

    def test_application_startup(self):
        """测试应用程序启动"""
        print("\n[测试 1] 应用程序启动...")
        try:
            if not os.path.exists(self.exe_path):
                self.results["failed"].append(("应用程序启动", "可执行文件不存在"))
                print("  ✗ 可执行文件不存在")
                return False

            if not PYWINAVAILABLE:
                self.results["skipped"].append(("应用程序启动", "pywinauto未安装"))
                print("  ⊘ 跳过 (需要pywinauto)")
                return None

            self.app = Application(backend="uia").start(self.exe_path)
            time.sleep(3)  # 等待应用启动

            if self.app.is_process_running():
                self.results["passed"].append(("应用程序启动", ""))
                print("  ✓ 应用程序成功启动")
                return True
            else:
                self.results["failed"].append(("应用程序启动", "进程未运行"))
                print("  ✗ 应用程序启动失败")
                return False
        except Exception as e:
            self.results["failed"].append(("应用程序启动", str(e)))
            print(f"  ✗ 启动失败: {e}")
            return False

    def test_window_visibility(self):
        """测试窗口可见性"""
        print("\n[测试 2] 窗口可见性...")
        try:
            if not self.app or not PYWINAVAILABLE:
                self.results["skipped"].append(("窗口可见性", "应用程序未启动或pywinauto未安装"))
                print("  ⊘ 跳过")
                return None

            main_window = self.app.top_window()
            if main_window.is_visible():
                self.results["passed"].append(("窗口可见性", ""))
                print("  ✓ 主窗口可见")
                return True
            else:
                self.results["failed"].append(("窗口可见性", "窗口不可见"))
                print("  ✗ 主窗口不可见")
                return False
        except Exception as e:
            self.results["failed"].append(("窗口可见性", str(e)))
            print(f"  ✗ 测试失败: {e}")
            return False

    def test_navigation_menu(self):
        """测试导航菜单"""
        print("\n[测试 3] 导航菜单...")
        try:
            if not self.app or not PYWINAVAILABLE:
                self.results["skipped"].append(("导航菜单", "应用程序未启动或pywinauto未安装"))
                print("  ⊘ 跳过")
                return None

            main_window = self.app.top_window()
            # 尝试查找导航菜单项
            # 注意: 这需要根据实际UI结构调整
            print("  ⊘ 跳过 (需要UI结构调整)")
            self.results["skipped"].append(("导航菜单", "需要UI结构调整"))
            return None
        except Exception as e:
            self.results["failed"].append(("导航菜单", str(e)))
            print(f"  ✗ 测试失败: {e}")
            return False

    def cleanup(self):
        """清理测试环境"""
        if self.app and PYWINAVAILABLE:
            try:
                self.app.kill()
                print("\n✓ 应用程序已关闭")
            except:
                pass

    def print_summary(self):
        """打印测试结果摘要"""
        print("\n" + "=" * 60)
        print("测试结果摘要")
        print("=" * 60)
        print(f"通过: {len(self.results['passed'])}")
        print(f"失败: {len(self.results['failed'])}")
        print(f"跳过: {len(self.results['skipped'])}")
        print()

        if self.results['failed']:
            print("失败的测试:")
            for test_name, reason in self.results['failed']:
                print(f"  - {test_name}: {reason}")
            print()

        return len(self.results['failed']) == 0


def main():
    print("=" * 60)
    print("Lenovo Legion Toolkit UI自动化测试")
    print("=" * 60)

    runner = UITestRunner()

    try:
        # 运行测试
        runner.test_application_startup()
        runner.test_window_visibility()
        runner.test_navigation_menu()

        # 打印摘要
        success = runner.print_summary()

        return 0 if success else 1
    finally:
        runner.cleanup()


if __name__ == "__main__":
    sys.exit(main())


