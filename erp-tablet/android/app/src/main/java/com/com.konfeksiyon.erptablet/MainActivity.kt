package com.konfeksiyon.erptablet

import android.os.Bundle
import android.view.KeyEvent
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.InputMethodManager
import android.widget.EditText
import com.facebook.react.ReactActivity
import com.facebook.react.ReactActivityDelegate
import com.facebook.react.ReactApplication
import com.facebook.react.defaults.DefaultNewArchitectureEntryPoint.fabricEnabled
import com.facebook.react.defaults.DefaultReactActivityDelegate

class MainActivity : ReactActivity() {

  /** Klavye gibi davranan kart okuyucunun Enter'a kadar yazdığı karakterler. */
  private val wedgeLine = StringBuilder()

  /**
   * Returns the name of the main component registered from JavaScript. This is used to schedule
   * rendering of the component.
   */
  override fun getMainComponentName(): String = "ErpTablet"

  /**
   * Returns the instance of the [ReactActivityDelegate]. We use [DefaultReactActivityDelegate]
   * which allows you to enable New Architecture with a single boolean flags [fabricEnabled]
   */
  override fun createReactActivityDelegate(): ReactActivityDelegate =
      DefaultReactActivityDelegate(this, mainComponentName, fabricEnabled)

  /**
   * Görünmez, odak tutabilen öğe: metin alanı odakta değilken odak hep bundadır. Odak hiçbir öğede değilken
   * (ör. ayarlardaki alan kapanınca) Android okuyucunun ilk tuşunu dokunmatik kipten çıkıp odak vermek için yutar;
   * kart numarasının ilk karakteri kaybolur ("Kart sistemde tanımlı değil", ikinci okutmada düzelir).
   */
  private lateinit var keyCatcher: View

  /**
   * Odak metin alanından başka bir öğeye geçince ekran klavyesi kapatılır; odak boşa düşerse tuş yakalayıcıya verilir.
   * Aksi halde ayarlardaki bir alandan çıkıldığında klavye istasyon ekranında açık kalır.
   */
  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)
    keyCatcher = View(this).apply {
      isFocusable = true
      isFocusableInTouchMode = true
      importantForAccessibility = View.IMPORTANT_FOR_ACCESSIBILITY_NO
    }
    addContentView(keyCatcher, ViewGroup.LayoutParams(1, 1))
    keyCatcher.requestFocus()

    window.decorView.viewTreeObserver.addOnGlobalFocusChangeListener { _, newFocus ->
      if (newFocus !is EditText) {
        hideSoftKeyboard()
      }
      if (newFocus == null) {
        keyCatcher.post { if (currentFocus == null) keyCatcher.requestFocus() }
      }
    }
  }

  private fun hideSoftKeyboard() {
    val imm = getSystemService(INPUT_METHOD_SERVICE) as InputMethodManager
    imm.hideSoftInputFromWindow(window.decorView.windowToken, 0)
  }

  /**
   * Kart okuyucu (USB / Bluetooth HID) klavye gibi kart numarasını yazıp Enter gönderir. Açık bir metin
   * alanı yoksa tuşlar burada toplanır ve satır JS'e "KeyboardWedgeLine" olayıyla iletilir. Gizli bir metin
   * alanı kullanılmadığı için klavye uygulaması devreye girmez; okutmada ekran klavyesi açılıp kapanmaz.
   */
  override fun dispatchKeyEvent(event: KeyEvent): Boolean {
    if (currentFocus is EditText) {
      return super.dispatchKeyEvent(event)
    }

    val isEnter = event.keyCode == KeyEvent.KEYCODE_ENTER || event.keyCode == KeyEvent.KEYCODE_NUMPAD_ENTER
    if (event.action == KeyEvent.ACTION_MULTIPLE && event.keyCode == KeyEvent.KEYCODE_UNKNOWN) {
      wedgeLine.append(event.characters ?: "")
      return true
    }

    val char = event.unicodeChar
    if (!isEnter && (char == 0 || Character.isISOControl(char))) {
      return super.dispatchKeyEvent(event)
    }

    if (event.action == KeyEvent.ACTION_DOWN) {
      // Okutma sırasında açık kalmış ya da açılmış ekran klavyesi kapatılır (yazılanlar zaten burada toplanır).
      hideSoftKeyboard()
      if (isEnter) {
        val line = wedgeLine.toString()
        wedgeLine.clear()
        if (line.isNotBlank()) {
          (application as ReactApplication).reactHost?.currentReactContext?.emitDeviceEvent("KeyboardWedgeLine", line)
        }
      } else {
        wedgeLine.appendCodePoint(char)
      }
    }
    return true
  }
}
